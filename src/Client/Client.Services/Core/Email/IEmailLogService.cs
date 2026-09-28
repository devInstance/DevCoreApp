using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;

namespace DevInstance.DevCoreApp.Client.Services.Core.Email;

/// <summary>Email log (<c>api/email-logs</c>). Same shape as the server service.</summary>
public interface IEmailLogService
{
    /// <param name="startDate">Local wall-clock time from a date filter; converted to UTC here.</param>
    /// <param name="endDate">Local wall-clock time from a date filter; converted to UTC here.</param>
    Task<ServiceActionResult<PagedList<EmailLogItem>>> GetAllAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null,
        string? search = null, int? status = null, string? templateName = null,
        DateTime? startDate = null, DateTime? endDate = null);

    Task<ServiceActionResult<EmailLogItem>> GetAsync(string id);
    Task<ServiceActionResult<EmailLogItem>> DeleteAsync(string id);
    Task<ServiceActionResult<bool>> DeleteMultipleAsync(List<string> publicIds);
    Task<ServiceActionResult<bool>> ResendAsync(string publicId);

    Task<ServiceActionResult<int>> ResendAllFailedAsync(
        int? status = null, DateTime? startDate = null, DateTime? endDate = null, string? search = null);
}
