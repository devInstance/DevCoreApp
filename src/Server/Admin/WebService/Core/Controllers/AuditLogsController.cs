using DevInstance.DevCoreApp.Server.Admin.Services.Core.AuditLogs;
using DevInstance.DevCoreApp.Shared.Model.Core.AuditLogs;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Core.Controllers;

[Route("api/audit-logs")]
[Authorize(Policy = PermissionDefinitions.System.AuditLog.View)]
public class AuditLogsController : ApiControllerBase
{
    private readonly IAuditLogService _service;

    public AuditLogsController(IAuditLogService service) => _service = service;

    [HttpGet]
    public Task<ActionResult<PagedList<AuditLogItem>>> GetListAsync([FromQuery] AuditLogQuery query)
        => HandleServiceAsync(() => _service.GetAllAsync(query.Top, query.Page, query.SortField, query.IsAsc,
            query.Search, query.Action, query.Source, query.TableName, query.RecordId, query.StartDate, query.EndDate));
}
