using DevInstance.DevCoreApp.Server.Services.Core.Email;
using DevInstance.DevCoreApp.Shared.Model.Core;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Api.Core.Controllers;

[Route("api/email-logs")]
[Authorize]
public class EmailLogsController : ApiControllerBase
{
    private readonly IEmailLogService _service;

    public EmailLogsController(IEmailLogService service) => _service = service;

    [HttpGet]
    [Authorize(Policy = PermissionDefinitions.System.EmailLog.View)]
    public Task<ActionResult<PagedList<EmailLogItem>>> GetListAsync([FromQuery] EmailLogQuery query)
        => HandleServiceAsync(() => _service.GetAllAsync(query.Top, query.Page, query.SortField, query.IsAsc,
            query.Search, query.Status, query.TemplateName, query.StartDate, query.EndDate));

    [HttpGet("{id}")]
    [Authorize(Policy = PermissionDefinitions.System.EmailLog.View)]
    public Task<ActionResult<EmailLogItem>> GetAsync(string id)
        => HandleServiceAsync(() => _service.GetAsync(id));

    // Deleting log rows has no permission of its own; the admin UI has always allowed it to
    // anyone who can view the log, so the API keeps that parity.
    [HttpDelete("{id}")]
    [Authorize(Policy = PermissionDefinitions.System.EmailLog.View)]
    public Task<ActionResult<EmailLogItem>> DeleteAsync(string id)
        => HandleServiceAsync(() => _service.DeleteAsync(id));

    /// <summary>Deletes several log rows; body is the list of ids.</summary>
    [HttpPost("bulk-delete")]
    [Authorize(Policy = PermissionDefinitions.System.EmailLog.View)]
    public Task<ActionResult<bool>> DeleteMultipleAsync([FromBody] List<string> ids)
        => HandleServiceAsync(() => _service.DeleteMultipleAsync(ids));

    [HttpPost("{id}/resend")]
    [Authorize(Policy = PermissionDefinitions.System.EmailLog.Resend)]
    public Task<ActionResult<bool>> ResendAsync(string id)
        => HandleServiceAsync(() => _service.ResendAsync(id));

    /// <summary>Resends every failed email matching the filters; returns how many were queued.</summary>
    [HttpPost("resend-failed")]
    [Authorize(Policy = PermissionDefinitions.System.EmailLog.Resend)]
    public Task<ActionResult<int>> ResendAllFailedAsync([FromQuery] EmailLogQuery query)
        => HandleServiceAsync(() => _service.ResendAllFailedAsync(query.Status, query.StartDate, query.EndDate, query.Search));
}
