using DevInstance.DevCoreApp.Server.Services.Core.Roles;
using DevInstance.DevCoreApp.Shared.Model.Core.Roles;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Api.Core.Controllers;

[Route("api/roles")]
[Authorize]
public class RolesController : ApiControllerBase
{
    private readonly IRoleManagementService _service;

    public RolesController(IRoleManagementService service) => _service = service;

    [HttpGet]
    [Authorize(Policy = PermissionDefinitions.Admin.Roles.View)]
    public Task<ActionResult<PagedList<RoleItem>>> GetListAsync([FromQuery] ListQuery query)
        => HandleServiceAsync(() => _service.GetRolesAsync(query.Top, query.Page, query.SortBy, query.Search));

    /// <summary>Every permission defined in the system, for the role editor.</summary>
    [HttpGet("~/api/permissions")]
    [Authorize(Policy = PermissionDefinitions.Admin.Roles.View)]
    public Task<ActionResult<List<PermissionItem>>> GetAllPermissionsAsync()
        => HandleServiceAsync(() => _service.GetAllPermissionsAsync());

    [HttpGet("{id}")]
    [Authorize(Policy = PermissionDefinitions.Admin.Roles.View)]
    public Task<ActionResult<RoleItem>> GetAsync(string id)
        => HandleServiceAsync(() => _service.GetRoleAsync(id));

    [HttpPost]
    [Authorize(Policy = PermissionDefinitions.Admin.Roles.Create)]
    public Task<ActionResult<RoleItem>> CreateAsync([FromBody] RoleItem item)
        => HandleServiceAsync(() => _service.CreateRoleAsync(item));

    [HttpPut("{id}")]
    [Authorize(Policy = PermissionDefinitions.Admin.Roles.Edit)]
    public Task<ActionResult<RoleItem>> UpdateAsync(string id, [FromBody] RoleItem item)
        => HandleServiceAsync(() => _service.UpdateRoleAsync(id, item));

    [HttpDelete("{id}")]
    [Authorize(Policy = PermissionDefinitions.Admin.Roles.Delete)]
    public Task<ActionResult<bool>> DeleteAsync(string id)
        => HandleServiceAsync(() => _service.DeleteRoleAsync(id));

    [HttpGet("{id}/permissions")]
    [Authorize(Policy = PermissionDefinitions.Admin.Roles.View)]
    public Task<ActionResult<List<string>>> GetPermissionKeysAsync(string id)
        => HandleServiceAsync(() => _service.GetRolePermissionKeysAsync(id));

    [HttpPut("{id}/permissions")]
    [Authorize(Policy = PermissionDefinitions.Admin.Roles.Edit)]
    public Task<ActionResult<bool>> SetPermissionsAsync(string id, [FromBody] RolePermissionsRequest request)
        => HandleServiceAsync(() => _service.SetRolePermissionsAsync(id, request));
}
