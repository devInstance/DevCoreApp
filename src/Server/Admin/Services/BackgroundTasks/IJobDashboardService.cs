using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.BackgroundTasks;
using DevInstance.WebServiceToolkit.Common.Model;

namespace DevInstance.DevCoreApp.Server.Admin.Services.BackgroundTasks;

public interface IJobDashboardService
{
    Task<ServiceActionResult<ModelList<BackgroundTaskItem>>> GetAllAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null,
        string? search = null, int? status = null, string? taskType = null,
        DateTime? startDate = null, DateTime? endDate = null);

    Task<ServiceActionResult<List<BackgroundTaskLogItem>>> GetJobLogsAsync(string jobPublicId);

    Task<ServiceActionResult<bool>> CancelJobAsync(string jobPublicId);

    Task<ServiceActionResult<bool>> RetryJobAsync(string jobPublicId);
}
