using System.Security.Cryptography;
using System.Text.Json;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Admin.Services.Background;
using DevInstance.DevCoreApp.Server.Admin.Services.Background.Requests;
using DevInstance.DevCoreApp.Server.Admin.Services.Files;
using DevInstance.DevCoreApp.Server.Admin.Services.ImportExport.Generation;
using DevInstance.DevCoreApp.Server.Admin.Services.ImportExport.Parsing;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Shared.Model.ImportExport;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace DevInstance.DevCoreApp.Server.Admin.Services.ImportExport;

[BlazorService]
public class ImportExportService : BaseService, IImportExportService
{
    private readonly IScopeLog log;
    private readonly IFileService FileService;
    private readonly IOperationContext OperationContext;
    private readonly IEnumerable<IImportHandler> ImportHandlers;
    private readonly IEnumerable<IExportHandler> ExportHandlers;
    private readonly IBackgroundWorker BackgroundWorker;
    private readonly IServiceProvider ServiceProvider;

    private const int BackgroundThreshold = 500;

    public ImportExportService(IScopeManager logManager,
                               ITimeProvider timeProvider,
                               IQueryRepository query,
                               IAuthorizationContext authorizationContext,
                               IFileService fileService,
                               IOperationContext operationContext,
                               IEnumerable<IImportHandler> importHandlers,
                               IEnumerable<IExportHandler> exportHandlers,
                               IBackgroundWorker backgroundWorker,
                               IServiceProvider serviceProvider)
        : base(logManager, timeProvider, query, authorizationContext)
    {
        log = logManager.CreateLogger(this);
        FileService = fileService;
        OperationContext = operationContext;
        ImportHandlers = importHandlers;
        ExportHandlers = exportHandlers;
        BackgroundWorker = backgroundWorker;
        ServiceProvider = serviceProvider;
    }

    // ── Import ──

    public ServiceActionResult<List<string>> GetImportableEntityTypes()
    {
        var types = ImportHandlers.Select(h => h.EntityType).Distinct().ToList();
        return ServiceActionResult<List<string>>.OK(types);
    }

    public ServiceActionResult<List<ImportFieldDescriptor>> GetImportFields(string entityType)
    {
        var handler = FindImportHandler(entityType);
        return ServiceActionResult<List<ImportFieldDescriptor>>.OK(handler.GetFieldDescriptors());
    }

    public async Task<ServiceActionResult<ImportParseResult>> ParseHeadersAsync(Stream fileStream, string fileName)
    {
        using var l = log.TraceScope();

        var format = FileParserFactory.DetectFormat(fileName);
        var parser = FileParserFactory.Create(format);

        var headers = await parser.ParseHeadersAsync(fileStream);
        fileStream.Position = 0;

        var rows = await parser.ParseRowsAsync(fileStream);
        fileStream.Position = 0;

        l.I($"Parsed headers from {fileName}: {headers.Count} columns, {rows.Count} rows.");

        return ServiceActionResult<ImportParseResult>.OK(new ImportParseResult
        {
            Headers = headers,
            RowCount = rows.Count,
            Format = format
        });
    }

    public bool RequiresOrganizationSelection()
    {
        return OperationContext.PrimaryOrganizationId == null;
    }

    public async Task<ServiceActionResult<ImportValidationResult>> ValidateAsync(
        Stream fileStream, string fileName, string entityType,
        List<ImportColumnMappingItem> mappings, string? organizationId = null)
    {
        using var l = log.TraceScope();

        var handler = FindImportHandler(entityType);
        var format = FileParserFactory.DetectFormat(fileName);

        // Resolve organization internal Guid if a PublicId was provided
        Guid? resolvedOrgId = OperationContext.PrimaryOrganizationId;
        if (!string.IsNullOrEmpty(organizationId))
        {
            var orgQuery = Repository.GetOrganizationsQuery(AuthorizationContext.CurrentProfile);
            var org = await orgQuery.ByPublicId(organizationId).Select().FirstOrDefaultAsync();
            if (org == null)
                throw new RecordNotFoundException($"Organization '{organizationId}' not found.");
            resolvedOrgId = org.Id;
        }

        // Compute file hash for duplicate detection
        fileStream.Position = 0;
        var fileHash = Convert.ToHexString(await SHA256.HashDataAsync(fileStream));
        fileStream.Position = 0;

        // Check for duplicate imports (same file + entity type in active state)
        var dupQuery = Repository.GetImportSessionQuery(AuthorizationContext.CurrentProfile);
        var duplicate = await dupQuery
            .ByFileHash(fileHash)
            .ByEntityType(entityType)
            .Select()
            .Where(s => s.Status != ImportSessionStatus.Failed
                      && s.Status != ImportSessionStatus.Cancelled
                      && s.Status != ImportSessionStatus.RolledBack)
            .FirstOrDefaultAsync();

        if (duplicate != null)
            throw new RecordConflictException(
                $"This file was already imported on {duplicate.CreateDate:g} (session {duplicate.PublicId}, status: {duplicate.Status}).");

        // Upload file via IFileService (deferred from Step 1)
        fileStream.Position = 0;
        var contentType = format == ImportFileFormat.Csv
            ? "text/csv"
            : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        var uploadResult = await FileService.UploadAsync(
            fileStream, fileName, contentType, "ImportSession", null, resolvedOrgId);
        var fileRecordId = uploadResult.Result?.Id;

        // Parse rows for validation
        fileStream.Position = 0;
        var parser = FileParserFactory.Create(format);
        var rows = await parser.ParseRowsAsync(fileStream);

        // Create ImportSession (deferred from Step 1)
        var sessionQuery = Repository.GetImportSessionQuery(AuthorizationContext.CurrentProfile);
        var session = sessionQuery.CreateNew();
        session.EntityType = entityType;
        session.OriginalFileName = fileName;
        session.FileFormat = format;
        session.Status = ImportSessionStatus.Mapped;
        session.FileRecordId = fileRecordId;
        session.TotalRows = rows.Count;
        session.ColumnMappingJson = JsonSerializer.Serialize(mappings);
        session.OrganizationId = resolvedOrgId ?? Guid.Empty;
        session.FileHash = fileHash;
        session.CreatedById = OperationContext.UserId;

        await sessionQuery.AddAsync(session);

        l.I($"Import session created: {session.PublicId} for {entityType}, {rows.Count} rows.");

        // Map and validate each row
        var validationResult = new ImportValidationResult
        {
            SessionId = session.PublicId,
            TotalRows = rows.Count
        };

        var activeMappings = mappings.Where(m => m.TargetField != null).ToList();
        var uniqueKeyField = handler.UniqueKeyField;
        var seenKeys = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < rows.Count; i++)
        {
            var mappedValues = new Dictionary<string, string?>();
            foreach (var mapping in activeMappings)
            {
                if (mapping.SourceColumnIndex >= 0 && mapping.SourceColumnIndex < rows[i].Length)
                {
                    mappedValues[mapping.TargetField!] = rows[i][mapping.SourceColumnIndex];
                }
            }

            var validation = await handler.ValidateRowAsync(mappedValues, ServiceProvider);

            // Detect within-file duplicates
            if (uniqueKeyField != null
                && mappedValues.TryGetValue(uniqueKeyField, out var keyValue)
                && !string.IsNullOrWhiteSpace(keyValue))
            {
                if (seenKeys.TryGetValue(keyValue, out var firstRow))
                {
                    validation.Warnings.Add($"Duplicate '{uniqueKeyField}' value '{keyValue}' — first seen in row {firstRow}.");
                }
                else
                {
                    seenKeys[keyValue] = i + 1;
                }
            }

            var status = validation.Errors.Count > 0
                ? ImportRowStatus.Error
                : validation.Warnings.Count > 0
                    ? ImportRowStatus.Warning
                    : ImportRowStatus.Valid;

            var preview = new ImportRowPreviewItem
            {
                RowNumber = i + 1,
                Values = mappedValues,
                Errors = validation.Errors,
                Warnings = validation.Warnings,
                Action = validation.Action,
                Status = status
            };

            validationResult.Rows.Add(preview);

            if (validation.Errors.Count > 0)
                validationResult.ErrorRows++;
            else if (validation.Warnings.Count > 0)
                validationResult.WarningRows++;
            else
                validationResult.ValidRows++;
        }

        // Update session with validation results
        session.Status = ImportSessionStatus.Validated;
        session.ValidRows = validationResult.ValidRows + validationResult.WarningRows;
        session.ErrorRows = validationResult.ErrorRows;
        session.ValidationResultJson = JsonSerializer.Serialize(
            validationResult.Rows.Where(r => r.Status == ImportRowStatus.Error).ToList());

        var updateQuery = Repository.GetImportSessionQuery(AuthorizationContext.CurrentProfile);
        await updateQuery.UpdateAsync(session);

        l.I($"Import session {session.PublicId} validated: {validationResult.ValidRows} valid, {validationResult.ErrorRows} errors.");

        return ServiceActionResult<ImportValidationResult>.OK(validationResult);
    }

    public async Task<ServiceActionResult<ImportCommitResult>> CommitAsync(string sessionId, List<int>? excludedRows = null)
    {
        using var l = log.TraceScope();

        var sessionQuery = Repository.GetImportSessionQuery(AuthorizationContext.CurrentProfile);
        var session = await sessionQuery.ByPublicId(sessionId).Select().FirstOrDefaultAsync();
        if (session == null)
            throw new RecordNotFoundException("Import session not found.");

        if (session.Status != ImportSessionStatus.Validated)
            throw new BadRequestException("Import session must be validated before committing.");

        // Persist excluded rows so background jobs also respect them
        if (excludedRows != null && excludedRows.Count > 0)
        {
            session.ExcludedRowsJson = JsonSerializer.Serialize(excludedRows);
            var saveQuery = Repository.GetImportSessionQuery(AuthorizationContext.CurrentProfile);
            await saveQuery.UpdateAsync(session);
        }

        // For large imports, submit as background job
        if (session.ValidRows > BackgroundThreshold)
        {
            session.Status = ImportSessionStatus.Processing;
            var updateQuery = Repository.GetImportSessionQuery(AuthorizationContext.CurrentProfile);
            await updateQuery.UpdateAsync(session);

            BackgroundWorker.Submit(new BackgroundRequestItem
            {
                RequestType = BackgroundRequestType.ImportData,
                Content = new ImportDataRequest { SessionId = sessionId }
            });

            l.I($"Import session {sessionId} submitted as background job ({session.ValidRows} rows).");

            return ServiceActionResult<ImportCommitResult>.OK(new ImportCommitResult
            {
                SessionId = sessionId,
                ImportedRows = 0,
                SkippedRows = session.ErrorRows,
                ErrorRows = 0,
                Errors = new List<string> { "Import is processing in the background." }
            });
        }

        // Inline commit for small imports
        return await CommitInternalAsync(session);
    }

    internal async Task<ServiceActionResult<ImportCommitResult>> CommitInternalAsync(
        DevInstance.DevCoreApp.Server.Database.Core.Models.ImportExport.ImportSession session)
    {
        using var l = log.TraceScope();

        var handler = FindImportHandler(session.EntityType);

        // Download and parse file
        if (string.IsNullOrEmpty(session.FileRecordId))
            throw new BadRequestException("No file associated with this import session.");

        var downloadResult = await FileService.DownloadAsync(session.FileRecordId);
        var fileStream = downloadResult.Result.Stream;

        var parser = FileParserFactory.Create(session.FileFormat);
        var rows = await parser.ParseRowsAsync(fileStream);

        var mappings = !string.IsNullOrEmpty(session.ColumnMappingJson)
            ? JsonSerializer.Deserialize<List<ImportColumnMappingItem>>(session.ColumnMappingJson) ?? new()
            : new List<ImportColumnMappingItem>();

        var activeMappings = mappings.Where(m => m.TargetField != null).ToList();

        // Load excluded rows (1-based row numbers)
        var excludedSet = new HashSet<int>();
        if (!string.IsNullOrEmpty(session.ExcludedRowsJson))
        {
            var excluded = JsonSerializer.Deserialize<List<int>>(session.ExcludedRowsJson);
            if (excluded != null) excludedSet = new HashSet<int>(excluded);
        }

        // Build valid rows
        var validRows = new List<Dictionary<string, string?>>();
        var skippedByExclusion = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            var rowNumber = i + 1;
            if (excludedSet.Contains(rowNumber))
            {
                skippedByExclusion++;
                continue;
            }

            var mappedValues = new Dictionary<string, string?>();
            foreach (var mapping in activeMappings)
            {
                if (mapping.SourceColumnIndex >= 0 && mapping.SourceColumnIndex < rows[i].Length)
                {
                    mappedValues[mapping.TargetField!] = rows[i][mapping.SourceColumnIndex];
                }
            }

            var validation = await handler.ValidateRowAsync(mappedValues, ServiceProvider);
            if (validation.Errors.Count == 0)
            {
                validRows.Add(mappedValues);
            }
        }

        // Commit
        try
        {
            session.Status = ImportSessionStatus.Processing;
            var updateQuery1 = Repository.GetImportSessionQuery(AuthorizationContext.CurrentProfile);
            await updateQuery1.UpdateAsync(session);

            var commitResult = await handler.CommitAsync(validRows, ServiceProvider);
            commitResult.SessionId = session.PublicId;
            commitResult.SkippedRows = session.ErrorRows + skippedByExclusion;

            session.ImportedRows = commitResult.ImportedRows;
            session.UpdatedRows = commitResult.UpdatedRows;
            session.Status = commitResult.ErrorRows > 0
                ? ImportSessionStatus.CompletedWithErrors
                : ImportSessionStatus.Completed;

            if (commitResult.ImportedRecordIds.Count > 0)
            {
                session.ImportedRecordIdsJson = JsonSerializer.Serialize(commitResult.ImportedRecordIds);
            }

            if (commitResult.Errors.Count > 0)
            {
                session.ErrorMessage = string.Join("; ", commitResult.Errors.Take(10));
            }

            var updateQuery2 = Repository.GetImportSessionQuery(AuthorizationContext.CurrentProfile);
            await updateQuery2.UpdateAsync(session);

            l.I($"Import session {session.PublicId} committed: {commitResult.ImportedRows} imported, {commitResult.ErrorRows} errors.");

            return ServiceActionResult<ImportCommitResult>.OK(commitResult);
        }
        catch (Exception ex)
        {
            session.Status = ImportSessionStatus.Failed;
            session.ErrorMessage = ex.Message;
            var updateQuery = Repository.GetImportSessionQuery(AuthorizationContext.CurrentProfile);
            await updateQuery.UpdateAsync(session);

            l.E($"Import session {session.PublicId} failed: {ex.Message}");
            throw;
        }
    }

    public async Task<ServiceActionResult<ImportSessionItem>> GetSessionAsync(string sessionId)
    {
        var sessionQuery = Repository.GetImportSessionQuery(AuthorizationContext.CurrentProfile);
        var session = await sessionQuery.ByPublicId(sessionId).Select().FirstOrDefaultAsync();
        if (session == null)
            throw new RecordNotFoundException("Import session not found.");

        return ServiceActionResult<ImportSessionItem>.OK(session.ToView());
    }

    public async Task<ServiceActionResult<bool>> RollbackAsync(string sessionId)
    {
        using var l = log.TraceScope();

        var sessionQuery = Repository.GetImportSessionQuery(AuthorizationContext.CurrentProfile);
        var session = await sessionQuery.ByPublicId(sessionId).Select().FirstOrDefaultAsync();
        if (session == null)
            throw new RecordNotFoundException("Import session not found.");

        if (session.Status != ImportSessionStatus.Completed && session.Status != ImportSessionStatus.CompletedWithErrors)
            throw new BadRequestException("Only completed import sessions can be rolled back.");

        if (string.IsNullOrEmpty(session.ImportedRecordIdsJson))
            throw new BadRequestException("No imported record IDs found for this session.");

        var recordIds = JsonSerializer.Deserialize<List<string>>(session.ImportedRecordIdsJson) ?? new();
        if (recordIds.Count == 0)
            throw new BadRequestException("No imported record IDs found for this session.");

        var handler = FindImportHandler(session.EntityType);
        await handler.RollbackAsync(recordIds, ServiceProvider);

        session.Status = ImportSessionStatus.RolledBack;
        var updateQuery = Repository.GetImportSessionQuery(AuthorizationContext.CurrentProfile);
        await updateQuery.UpdateAsync(session);

        l.I($"Import session {sessionId} rolled back ({recordIds.Count} records).");

        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<ExportDownloadResult>> GetTemplateAsync(string entityType, ExportFileFormat format)
    {
        using var l = log.TraceScope();

        var handler = FindImportHandler(entityType);
        var fields = handler.GetFieldDescriptors();

        var headers = fields.Select(f => f.Label).ToList();

        // Create one example row with placeholder values based on DataType
        var exampleRow = new Dictionary<string, string?>();
        foreach (var field in fields)
        {
            exampleRow[field.Label] = field.DataType switch
            {
                "email" => "user@example.com",
                "phone" => "+1 (555) 000-0000",
                _ => field.IsRequired ? field.Label : ""
            };
        }

        IFileGenerator generator = format switch
        {
            ExportFileFormat.Csv => new CsvFileGenerator(),
            ExportFileFormat.Xlsx => new ExcelFileGenerator(),
            _ => throw new BadRequestException($"Unsupported format: {format}")
        };

        var stream = await generator.GenerateAsync(headers, new List<Dictionary<string, string?>> { exampleRow });

        var (contentType, extension) = format switch
        {
            ExportFileFormat.Csv => ("text/csv", ".csv"),
            ExportFileFormat.Xlsx => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx"),
            _ => ("application/octet-stream", ".bin")
        };

        var fileName = $"{entityType}_Template{extension}";

        l.I($"Template generated: {fileName}");

        return ServiceActionResult<ExportDownloadResult>.OK(new ExportDownloadResult
        {
            Stream = stream,
            ContentType = contentType,
            FileName = fileName
        });
    }

    // ── Export ──

    public ServiceActionResult<List<string>> GetExportableEntityTypes()
    {
        var types = ExportHandlers.Select(h => h.EntityType).Distinct().ToList();
        return ServiceActionResult<List<string>>.OK(types);
    }

    public ServiceActionResult<List<ExportFieldDescriptor>> GetExportFields(string entityType)
    {
        var handler = FindExportHandler(entityType);
        return ServiceActionResult<List<ExportFieldDescriptor>>.OK(handler.GetFieldDescriptors());
    }

    public async Task<ServiceActionResult<ExportDownloadResult>> ExportAsync(ExportRequestItem request)
    {
        using var l = log.TraceScope();

        var handler = FindExportHandler(request.EntityType);

        var selectedFields = request.SelectedFields.Count > 0
            ? request.SelectedFields
            : handler.GetFieldDescriptors().Where(f => f.IsDefault).Select(f => f.Field).ToList();

        var data = await handler.GetExportDataAsync(selectedFields, request.Search, request.SortBy, ServiceProvider);

        // Map field names to labels for headers
        var fieldDescriptors = handler.GetFieldDescriptors();
        var headers = selectedFields
            .Select(f => fieldDescriptors.FirstOrDefault(fd => fd.Field == f)?.Label ?? f)
            .ToList();

        // Re-key data dictionaries to use labels instead of field names
        var labeledData = new List<Dictionary<string, string?>>();
        foreach (var row in data)
        {
            var labeledRow = new Dictionary<string, string?>();
            foreach (var field in selectedFields)
            {
                var label = fieldDescriptors.FirstOrDefault(fd => fd.Field == field)?.Label ?? field;
                row.TryGetValue(field, out var value);
                labeledRow[label] = value;
            }
            labeledData.Add(labeledRow);
        }

        IFileGenerator generator = request.Format switch
        {
            ExportFileFormat.Csv => new CsvFileGenerator(),
            ExportFileFormat.Xlsx => new ExcelFileGenerator(),
            _ => throw new BadRequestException($"Unsupported export format: {request.Format}")
        };

        var stream = await generator.GenerateAsync(headers, labeledData);

        var (contentType, extension) = request.Format switch
        {
            ExportFileFormat.Csv => ("text/csv", ".csv"),
            ExportFileFormat.Xlsx => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx"),
            _ => ("application/octet-stream", ".bin")
        };

        var fileName = $"{request.EntityType}_Export_{DateTime.UtcNow:yyyyMMdd_HHmmss}{extension}";

        l.I($"Export generated: {fileName} ({data.Count} rows).");

        return ServiceActionResult<ExportDownloadResult>.OK(new ExportDownloadResult
        {
            Stream = stream,
            ContentType = contentType,
            FileName = fileName
        });
    }

    // ── Helpers ──

    private IImportHandler FindImportHandler(string entityType)
    {
        return ImportHandlers.FirstOrDefault(h =>
            string.Equals(h.EntityType, entityType, StringComparison.OrdinalIgnoreCase))
            ?? throw new RecordNotFoundException($"No import handler found for entity type '{entityType}'.");
    }

    private IExportHandler FindExportHandler(string entityType)
    {
        return ExportHandlers.FirstOrDefault(h =>
            string.Equals(h.EntityType, entityType, StringComparison.OrdinalIgnoreCase))
            ?? throw new RecordNotFoundException($"No export handler found for entity type '{entityType}'.");
    }
}
