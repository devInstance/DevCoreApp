using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core.ImportExport;

namespace DevInstance.DevCoreApp.Client.Services.Core.ImportExport;

/// <summary>
/// Import/export engine (<c>api/import-export</c>). Same shape as the server service, except
/// that the server's synchronous discovery methods are async here — they are HTTP calls.
/// </summary>
public interface IImportExportService
{
    // Import
    Task<ServiceActionResult<List<string>>> GetImportableEntityTypesAsync();
    Task<ServiceActionResult<List<ImportFieldDescriptor>>> GetImportFieldsAsync(string entityType);
    Task<ServiceActionResult<bool>> RequiresOrganizationSelectionAsync();
    Task<ServiceActionResult<ImportParseResult>> ParseHeadersAsync(Stream fileStream, string fileName);
    Task<ServiceActionResult<ImportValidationResult>> ValidateAsync(
        Stream fileStream, string fileName, string entityType,
        List<ImportColumnMappingItem> mappings, string? organizationId = null);
    Task<ServiceActionResult<ImportCommitResult>> CommitAsync(string sessionId, List<int>? excludedRows = null);
    Task<ServiceActionResult<bool>> RollbackAsync(string sessionId);
    Task<ServiceActionResult<ImportSessionItem>> GetSessionAsync(string sessionId);

    // Export
    Task<ServiceActionResult<List<ExportFieldDescriptor>>> GetExportFieldsAsync(string entityType);
    Task<ServiceActionResult<ExportDownloadResult>> ExportAsync(ExportRequestItem request);
}
