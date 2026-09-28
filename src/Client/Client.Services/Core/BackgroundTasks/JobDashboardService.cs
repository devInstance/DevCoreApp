using DevInstance.BlazorToolkit.Http;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Client.Services.Core.Time;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.LogScope;
using DevInstance.DevCoreApp.Shared.Model.Core.BackgroundTasks;

namespace DevInstance.DevCoreApp.Client.Services.Core.BackgroundTasks;

[BlazorService]
public class JobDashboardService : ApiServiceBase, IJobDashboardService
{
    private readonly ILocalTimeService time;

    public JobDashboardService(IHttpApiContextFactory apiFactory, ILocalTimeService time, IScopeManager logManager)
        : base(apiFactory, logManager)
    {
        this.time = time;
    }

    public Task<ServiceActionResult<PagedList<BackgroundTaskItem>>> GetAllAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null,
        string? search = null, int? status = null, string? taskType = null,
        DateTime? startDate = null, DateTime? endDate = null) =>
        CallAsync(() => Api<BackgroundTaskItem>("api/jobs").Get()
            .Query(new JobQuery
            {
                Top = top ?? 20, Page = page ?? 0, SortBy = SortBy(sortField, isAsc)!, Search = search!,
                Status = status, TaskType = taskType!,
                StartDate = time.ToUtc(startDate), EndDate = time.ToUtc(endDate)
            })
            .ExecuteAsync<PagedList<BackgroundTaskItem>>());

    public Task<ServiceActionResult<List<BackgroundTaskLogItem>>> GetJobLogsAsync(string jobPublicId) =>
        CallAsync(() => Api<List<BackgroundTaskLogItem>>($"api/jobs/{Segment(jobPublicId)}/logs").Get().ExecuteAsync());

    public Task<ServiceActionResult<bool>> CancelJobAsync(string jobPublicId) =>
        CallAsync(() => Api<bool>($"api/jobs/{Segment(jobPublicId)}/cancel").Post<object?>(null).ExecuteAsync());

    public Task<ServiceActionResult<bool>> RetryJobAsync(string jobPublicId) =>
        CallAsync(() => Api<bool>($"api/jobs/{Segment(jobPublicId)}/retry").Post<object?>(null).ExecuteAsync());
}
