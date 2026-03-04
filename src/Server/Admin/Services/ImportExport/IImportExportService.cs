using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.ImportExport;

namespace DevInstance.DevCoreApp.Server.Admin.Services.ImportExport;

public interface IImportExportService
{
    // Import
    ServiceActionResult<List<string>> GetImportableEntityTypes();
    ServiceActionResult<List<ImportFieldDescriptor>> GetImportFields(string entityType);
    Task<ServiceActionResult<ImportParseResult>> ParseHeadersAsync(Stream fileStream, string fileName);
    bool RequiresOrganizationSelection();
    Task<ServiceActionResult<ImportValidationResult>> ValidateAsync(
        Stream fileStream, string fileName, string entityType,
        List<ImportColumnMappingItem> mappings, string? organizationId = null);
    Task<ServiceActionResult<ImportCommitResult>> CommitAsync(string sessionId, List<int>? excludedRows = null);
    Task<ServiceActionResult<bool>> RollbackAsync(string sessionId);
    Task<ServiceActionResult<ExportDownloadResult>> GetTemplateAsync(string entityType, ExportFileFormat format);
    Task<ServiceActionResult<ImportSessionItem>> GetSessionAsync(string sessionId);

    // Export
    ServiceActionResult<List<string>> GetExportableEntityTypes();
    ServiceActionResult<List<ExportFieldDescriptor>> GetExportFields(string entityType);
    Task<ServiceActionResult<ExportDownloadResult>> ExportAsync(ExportRequestItem request);
}
