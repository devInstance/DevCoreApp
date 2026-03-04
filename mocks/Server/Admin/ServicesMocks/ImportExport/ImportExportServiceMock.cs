using Bogus;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.ImportExport;
using DevInstance.DevCoreApp.Shared.Model.ImportExport;
using DevInstance.WebServiceToolkit.Common.Tools;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Mocks.ImportExport;

[BlazorServiceMock]
public class ImportExportServiceMock : IImportExportService
{
    private int delay = 500;

    private readonly List<ImportFieldDescriptor> userProfileImportFields = new()
    {
        new() { Field = "Email", Label = "Email", IsRequired = true, DataType = "email", Description = "User email address" },
        new() { Field = "FirstName", Label = "First Name", IsRequired = true, DataType = "string" },
        new() { Field = "MiddleName", Label = "Middle Name", IsRequired = false, DataType = "string" },
        new() { Field = "LastName", Label = "Last Name", IsRequired = true, DataType = "string" },
        new() { Field = "PhoneNumber", Label = "Phone Number", IsRequired = false, DataType = "phone" },
        new() { Field = "Role", Label = "Role", IsRequired = true, DataType = "string", Description = "Admin, Manager, Employee, or Client" }
    };

    private readonly List<ExportFieldDescriptor> userProfileExportFields = new()
    {
        new() { Field = "Email", Label = "Email", IsDefault = true },
        new() { Field = "FirstName", Label = "First Name", IsDefault = true },
        new() { Field = "MiddleName", Label = "Middle Name", IsDefault = false },
        new() { Field = "LastName", Label = "Last Name", IsDefault = true },
        new() { Field = "PhoneNumber", Label = "Phone Number", IsDefault = true },
        new() { Field = "Roles", Label = "Roles", IsDefault = true },
        new() { Field = "Status", Label = "Status", IsDefault = true }
    };

    private ImportSessionItem? currentSession;
    private ImportValidationResult? lastValidation;

    // ── Import ──

    public ServiceActionResult<List<string>> GetImportableEntityTypes()
    {
        return ServiceActionResult<List<string>>.OK(new List<string> { "UserProfile" });
    }

    public ServiceActionResult<List<ImportFieldDescriptor>> GetImportFields(string entityType)
    {
        return ServiceActionResult<List<ImportFieldDescriptor>>.OK(userProfileImportFields);
    }

    public async Task<ServiceActionResult<ImportParseResult>> ParseHeadersAsync(Stream fileStream, string fileName)
    {
        await Task.Delay(delay);

        var format = fileName.EndsWith(".xlsx") ? ImportFileFormat.Xlsx : ImportFileFormat.Csv;
        return ServiceActionResult<ImportParseResult>.OK(new ImportParseResult
        {
            Headers = new List<string> { "Email", "First Name", "Middle Name", "Last Name", "Phone", "Role", "Department" },
            RowCount = 15,
            Format = format
        });
    }

    public bool RequiresOrganizationSelection()
    {
        return false;
    }

    public async Task<ServiceActionResult<ImportValidationResult>> ValidateAsync(
        Stream fileStream, string fileName, string entityType,
        List<ImportColumnMappingItem> mappings, string? organizationId = null)
    {
        await Task.Delay(delay * 2);

        var sessionId = IdGenerator.New();

        currentSession = new ImportSessionItem
        {
            Id = sessionId,
            EntityType = entityType,
            OriginalFileName = fileName,
            FileFormat = fileName.EndsWith(".xlsx") ? ImportFileFormat.Xlsx : ImportFileFormat.Csv,
            Status = ImportSessionStatus.Validated,
            TotalRows = 15,
            ValidRows = 12,
            ErrorRows = 3,
            CreateDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow
        };

        var faker = new Faker();
        var rows = new List<ImportRowPreviewItem>();

        for (int i = 1; i <= 15; i++)
        {
            var hasError = i % 5 == 0; // Every 5th row has an error
            var isUpdate = i % 3 == 0 && !hasError; // Every 3rd row (non-error) is an update
            var row = new ImportRowPreviewItem
            {
                RowNumber = i,
                Status = hasError ? ImportRowStatus.Error : isUpdate ? ImportRowStatus.Warning : ImportRowStatus.Valid,
                Action = isUpdate ? ImportRowAction.Update : ImportRowAction.Create,
                Values = new Dictionary<string, string?>
                {
                    { "Email", hasError ? "invalid-email" : faker.Internet.Email() },
                    { "FirstName", faker.Name.FirstName() },
                    { "LastName", faker.Name.LastName() },
                    { "Role", hasError ? "InvalidRole" : faker.PickRandom("Admin", "Manager", "Employee", "Client") }
                },
                Errors = hasError
                    ? new List<string> { "Email format is invalid.", "Role must be one of: Admin, Manager, Employee, Client." }
                    : new List<string>(),
                Warnings = isUpdate
                    ? new List<string> { "User already exists and will be updated." }
                    : new List<string>()
            };
            rows.Add(row);
        }

        lastValidation = new ImportValidationResult
        {
            SessionId = sessionId,
            TotalRows = 15,
            ValidRows = 8,
            WarningRows = 4,
            ErrorRows = 3,
            Rows = rows
        };

        return ServiceActionResult<ImportValidationResult>.OK(lastValidation);
    }

    public async Task<ServiceActionResult<ImportCommitResult>> CommitAsync(string sessionId, List<int>? excludedRows = null)
    {
        await Task.Delay(delay * 2);

        if (currentSession != null)
        {
            currentSession.Status = ImportSessionStatus.Completed;
            currentSession.ImportedRows = 8;
            currentSession.UpdatedRows = 4;
        }

        var importedIds = Enumerable.Range(1, 8).Select(_ => IdGenerator.New()).ToList();

        return ServiceActionResult<ImportCommitResult>.OK(new ImportCommitResult
        {
            SessionId = sessionId,
            ImportedRows = 8,
            UpdatedRows = 4,
            SkippedRows = 3,
            ErrorRows = 0,
            ImportedRecordIds = importedIds,
            Errors = new List<string>()
        });
    }

    public async Task<ServiceActionResult<bool>> RollbackAsync(string sessionId)
    {
        await Task.Delay(delay);

        if (currentSession != null)
        {
            currentSession.Status = ImportSessionStatus.RolledBack;
        }

        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<ExportDownloadResult>> GetTemplateAsync(string entityType, ExportFileFormat format)
    {
        await Task.Delay(delay);

        var fields = userProfileImportFields;
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);

        var headers = fields.Select(f => f.Label).ToList();
        await writer.WriteLineAsync(string.Join(",", headers));

        // Example row
        var exampleValues = fields.Select(f => f.DataType switch
        {
            "email" => "user@example.com",
            "phone" => "+1 (555) 000-0000",
            _ => f.IsRequired ? f.Label : ""
        });
        await writer.WriteLineAsync(string.Join(",", exampleValues));

        await writer.FlushAsync();
        stream.Position = 0;

        var extension = format == ExportFileFormat.Xlsx ? ".xlsx" : ".csv";
        var contentType = format == ExportFileFormat.Xlsx
            ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            : "text/csv";

        return ServiceActionResult<ExportDownloadResult>.OK(new ExportDownloadResult
        {
            Stream = stream,
            ContentType = contentType,
            FileName = $"{entityType}_Template{extension}"
        });
    }

    public async Task<ServiceActionResult<ImportSessionItem>> GetSessionAsync(string sessionId)
    {
        await Task.Delay(delay);

        if (currentSession != null && currentSession.Id == sessionId)
        {
            return ServiceActionResult<ImportSessionItem>.OK(currentSession);
        }

        return ServiceActionResult<ImportSessionItem>.OK(new ImportSessionItem
        {
            Id = sessionId,
            EntityType = "UserProfile",
            OriginalFileName = "users.csv",
            Status = ImportSessionStatus.Completed,
            TotalRows = 15,
            ValidRows = 12,
            ErrorRows = 3,
            ImportedRows = 8,
            UpdatedRows = 4,
            CreateDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow
        });
    }

    // ── Export ──

    public ServiceActionResult<List<string>> GetExportableEntityTypes()
    {
        return ServiceActionResult<List<string>>.OK(new List<string> { "UserProfile" });
    }

    public ServiceActionResult<List<ExportFieldDescriptor>> GetExportFields(string entityType)
    {
        return ServiceActionResult<List<ExportFieldDescriptor>>.OK(userProfileExportFields);
    }

    public async Task<ServiceActionResult<ExportDownloadResult>> ExportAsync(ExportRequestItem request)
    {
        await Task.Delay(delay);

        var faker = new Faker();
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);

        var fields = request.SelectedFields.Count > 0
            ? request.SelectedFields
            : userProfileExportFields.Where(f => f.IsDefault).Select(f => f.Field).ToList();

        await writer.WriteLineAsync(string.Join(",", fields));
        for (int i = 0; i < 20; i++)
        {
            var values = fields.Select<string, string>(f => f switch
            {
                "Email" => faker.Internet.Email(),
                "FirstName" => faker.Name.FirstName(),
                "MiddleName" => faker.Name.FirstName(),
                "LastName" => faker.Name.LastName(),
                "PhoneNumber" => faker.Phone.PhoneNumber(),
                "Roles" => faker.PickRandom("Admin", "Manager", "Employee", "Client"),
                "Status" => faker.PickRandom("Active", "Suspended"),
                _ => ""
            });
            await writer.WriteLineAsync(string.Join(",", values));
        }
        await writer.FlushAsync();
        stream.Position = 0;

        var extension = request.Format == ExportFileFormat.Xlsx ? ".xlsx" : ".csv";
        var contentType = request.Format == ExportFileFormat.Xlsx
            ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            : "text/csv";

        return ServiceActionResult<ExportDownloadResult>.OK(new ExportDownloadResult
        {
            Stream = stream,
            ContentType = contentType,
            FileName = $"{request.EntityType}_Export_{DateTime.UtcNow:yyyyMMdd_HHmmss}{extension}"
        });
    }
}
