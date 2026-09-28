using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core.BackgroundTasks;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;

namespace DevInstance.DevCoreApp.Client.Services.Core.BackgroundTasks;

/// <summary>Background job dashboard (<c>api/jobs</c>). Same shape as the server service.</summary>
public interface IJobDashboardService
{
    /// <param name="startDate">Local wall-clock time from a date filter; converted to UTC here.</param>
    /// <param name="endDate">Local wall-clock time from a date filter; converted to UTC here.</param>
    Task<ServiceActionResult<PagedList<BackgroundTaskItem>>> GetAllAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null,
        string? search = null, int? status = null, string? taskType = null,
        DateTime? startDate = null, DateTime? endDate = null);

    Task<ServiceActionResult<List<BackgroundTaskLogItem>>> GetJobLogsAsync(string jobPublicId);
    Task<ServiceActionResult<bool>> CancelJobAsync(string jobPublicId);
    Task<ServiceActionResult<bool>> RetryJobAsync(string jobPublicId);
}
