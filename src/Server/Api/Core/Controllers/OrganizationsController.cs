using DevInstance.DevCoreApp.Server.Services.Core.Organizations;
using DevInstance.DevCoreApp.Shared.Model.Core.Organizations;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Api.Core.Controllers;

[Route("api/organizations")]
[Authorize]
public class OrganizationsController : ApiControllerBase
{
    private readonly IOrganizationService _service;

    public OrganizationsController(IOrganizationService service) => _service = service;

    [HttpGet]
    [Authorize(Policy = PermissionDefinitions.Admin.Organizations.View)]
    public Task<ActionResult<PagedList<OrganizationItem>>> GetListAsync([FromQuery] OrganizationQuery query)
        => HandleServiceAsync(() => _service.GetAllAsync(query.Top, query.Page, query.SortField, query.IsAsc, query.Search, query.IsActive));

    /// <summary>The organizations visible to the caller as a tree (roots with nested children).</summary>
    [HttpGet("tree")]
    [Authorize(Policy = PermissionDefinitions.Admin.Organizations.View)]
    public Task<ActionResult<List<OrganizationItem>>> GetTreeAsync()
        => HandleServiceAsync(() => _service.GetTreeAsync());

    /// <summary>The caller's primary organization; null when they have none.</summary>
    [HttpGet("current")]
    public Task<ActionResult<OrganizationItem?>> GetCurrentAsync()
        => HandleServiceAsync(() => _service.GetCurrentAsync());

    [HttpGet("{id}")]
    [Authorize(Policy = PermissionDefinitions.Admin.Organizations.View)]
    public Task<ActionResult<OrganizationItem>> GetAsync(string id)
        => HandleServiceAsync(() => _service.GetAsync(id));

    [HttpPost]
    [Authorize(Policy = PermissionDefinitions.Admin.Organizations.Create)]
    public Task<ActionResult<OrganizationItem>> CreateAsync([FromBody] OrganizationItem item, [FromQuery] string? parentId = null)
        => HandleServiceAsync(() => _service.CreateAsync(item, parentId));

    [HttpPut("{id}")]
    [Authorize(Policy = PermissionDefinitions.Admin.Organizations.Edit)]
    public Task<ActionResult<OrganizationItem>> UpdateAsync(string id, [FromBody] OrganizationItem item)
        => HandleServiceAsync(() => _service.UpdateAsync(id, item));

    [HttpPost("{id}/toggle-active")]
    [Authorize(Policy = PermissionDefinitions.Admin.Organizations.Edit)]
    public Task<ActionResult<OrganizationItem>> ToggleActiveAsync(string id)
        => HandleServiceAsync(() => _service.ToggleActiveAsync(id));

    [HttpPost("{id}/move")]
    [Authorize(Policy = PermissionDefinitions.Admin.Organizations.Edit)]
    public Task<ActionResult<OrganizationItem>> MoveAsync(string id, [FromQuery] string parentId)
        => HandleServiceAsync(() => _service.MoveAsync(id, parentId));
}
