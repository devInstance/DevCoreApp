using DevInstance.DevCoreApp.Server.Services.Core.FeatureFlags;
using DevInstance.DevCoreApp.Shared.Model.Core.FeatureFlags;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Api.Core.Controllers;

[Route("api/feature-flags")]
[Authorize]
public class FeatureFlagsController : ApiControllerBase
{
    private readonly IFeatureFlagAdminService _service;

    public FeatureFlagsController(IFeatureFlagAdminService service) => _service = service;

    [HttpGet]
    [Authorize(Policy = PermissionDefinitions.Admin.FeatureFlags.View)]
    public Task<ActionResult<PagedList<FeatureFlagItem>>> GetListAsync([FromQuery] ListQuery query)
        => HandleServiceAsync(() => _service.GetFlagsAsync(query.Top, query.Page, query.SortBy, query.Search));

    [HttpGet("{id}")]
    [Authorize(Policy = PermissionDefinitions.Admin.FeatureFlags.View)]
    public Task<ActionResult<FeatureFlagItem>> GetAsync(string id)
        => HandleServiceAsync(() => _service.GetFlagAsync(id));

    [HttpPost]
    [Authorize(Policy = PermissionDefinitions.Admin.FeatureFlags.Create)]
    public Task<ActionResult<FeatureFlagItem>> CreateAsync([FromBody] FeatureFlagItem item)
        => HandleServiceAsync(() => _service.CreateFlagAsync(item));

    [HttpPut("{id}")]
    [Authorize(Policy = PermissionDefinitions.Admin.FeatureFlags.Edit)]
    public Task<ActionResult<FeatureFlagItem>> UpdateAsync(string id, [FromBody] FeatureFlagItem item)
        => HandleServiceAsync(() => _service.UpdateFlagAsync(id, item));

    [HttpDelete("{id}")]
    [Authorize(Policy = PermissionDefinitions.Admin.FeatureFlags.Delete)]
    public Task<ActionResult<bool>> DeleteAsync(string id)
        => HandleServiceAsync(() => _service.DeleteFlagAsync(id));
}
