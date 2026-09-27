using DevInstance.DevCoreApp.Server.Services.Core.BackgroundTasks;
using DevInstance.DevCoreApp.Shared.Model.Core.BackgroundTasks;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Api.Core.Controllers;

[Route("api/jobs")]
[Authorize]
public class JobsController : ApiControllerBase
{
    private readonly IJobDashboardService _service;

    public JobsController(IJobDashboardService service) => _service = service;

    [HttpGet]
    [Authorize(Policy = PermissionDefinitions.System.Jobs.View)]
    public Task<ActionResult<PagedList<BackgroundTaskItem>>> GetListAsync([FromQuery] JobQuery query)
        => HandleServiceAsync(() => _service.GetAllAsync(query.Top, query.Page, query.SortField, query.IsAsc,
            query.Search, query.Status, query.TaskType, query.StartDate, query.EndDate));

    [HttpGet("{id}/logs")]
    [Authorize(Policy = PermissionDefinitions.System.Jobs.View)]
    public Task<ActionResult<List<BackgroundTaskLogItem>>> GetLogsAsync(string id)
        => HandleServiceAsync(() => _service.GetJobLogsAsync(id));

    [HttpPost("{id}/cancel")]
    [Authorize(Policy = PermissionDefinitions.System.Jobs.Cancel)]
    public Task<ActionResult<bool>> CancelAsync(string id)
        => HandleServiceAsync(() => _service.CancelJobAsync(id));

    [HttpPost("{id}/retry")]
    [Authorize(Policy = PermissionDefinitions.System.Jobs.Retry)]
    public Task<ActionResult<bool>> RetryAsync(string id)
        => HandleServiceAsync(() => _service.RetryJobAsync(id));
}
