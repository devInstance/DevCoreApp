using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.ImportExport;

namespace DevInstance.DevCoreApp.Server.Admin.Services.ImportExport;

public interface IImportExportService
{
    // Import
    ServiceActionResult<List<string>> GetImportableEntityTypes();
    ServiceActionResult<List<ImportFieldDescriptor>> GetImportFields(string entityType);
    Task<ServiceActionResult<(List<string> Headers, ImportSessionItem Session)>> ParseFileAsync(
        Stream fileStream, string fileName, string entityType);
    Task<ServiceActionResult<ImportValidationResult>> ValidateAsync(
        string sessionId, List<ImportColumnMappingItem> mappings);
    Task<ServiceActionResult<ImportCommitResult>> CommitAsync(string sessionId);
    Task<ServiceActionResult<ImportSessionItem>> GetSessionAsync(string sessionId);

    // Export
    ServiceActionResult<List<string>> GetExportableEntityTypes();
    ServiceActionResult<List<ExportFieldDescriptor>> GetExportFields(string entityType);
    Task<ServiceActionResult<ExportDownloadResult>> ExportAsync(ExportRequestItem request);
}
