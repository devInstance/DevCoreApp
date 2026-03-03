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

    public async Task<ServiceActionResult<(List<string> Headers, ImportSessionItem Session)>> ParseFileAsync(
        Stream fileStream, string fileName, string entityType)
    {
        await Task.Delay(delay);

        var headers = new List<string> { "Email", "First Name", "Middle Name", "Last Name", "Phone", "Role" };

        currentSession = new ImportSessionItem
        {
            Id = IdGenerator.New(),
            EntityType = entityType,
            OriginalFileName = fileName,
            FileFormat = fileName.EndsWith(".xlsx") ? ImportFileFormat.Xlsx : ImportFileFormat.Csv,
            Status = ImportSessionStatus.Uploaded,
            TotalRows = 15,
            CreateDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow
        };

        return ServiceActionResult<(List<string> Headers, ImportSessionItem Session)>.OK(
            (headers, currentSession));
    }

    public async Task<ServiceActionResult<ImportValidationResult>> ValidateAsync(
        string sessionId, List<ImportColumnMappingItem> mappings)
    {
        await Task.Delay(delay * 2);

        var faker = new Faker();
        var rows = new List<ImportRowPreviewItem>();

        for (int i = 1; i <= 15; i++)
        {
            var hasError = i % 5 == 0; // Every 5th row has an error
            var row = new ImportRowPreviewItem
            {
                RowNumber = i,
                Status = hasError ? ImportRowStatus.Error : ImportRowStatus.Valid,
                Values = new Dictionary<string, string?>
                {
                    { "Email", hasError ? "invalid-email" : faker.Internet.Email() },
                    { "FirstName", faker.Name.FirstName() },
                    { "LastName", faker.Name.LastName() },
                    { "Role", hasError ? "InvalidRole" : faker.PickRandom("Admin", "Manager", "Employee", "Client") }
                },
                Errors = hasError
                    ? new List<string> { "Email format is invalid.", "Role must be one of: Admin, Manager, Employee, Client." }
                    : new List<string>()
            };
            rows.Add(row);
        }

        lastValidation = new ImportValidationResult
        {
            SessionId = sessionId,
            TotalRows = 15,
            ValidRows = 12,
            ErrorRows = 3,
            Rows = rows
        };

        if (currentSession != null)
        {
            currentSession.Status = ImportSessionStatus.Validated;
            currentSession.ValidRows = 12;
            currentSession.ErrorRows = 3;
        }

        return ServiceActionResult<ImportValidationResult>.OK(lastValidation);
    }

    public async Task<ServiceActionResult<ImportCommitResult>> CommitAsync(string sessionId)
    {
        await Task.Delay(delay * 2);

        if (currentSession != null)
        {
            currentSession.Status = ImportSessionStatus.Completed;
            currentSession.ImportedRows = 12;
        }

        return ServiceActionResult<ImportCommitResult>.OK(new ImportCommitResult
        {
            SessionId = sessionId,
            ImportedRows = 12,
            SkippedRows = 3,
            ErrorRows = 0,
            Errors = new List<string>()
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
            ImportedRows = 12,
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
