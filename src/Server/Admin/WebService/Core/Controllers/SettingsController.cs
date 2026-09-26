using DevInstance.DevCoreApp.Server.Admin.Services.Core.Settings;
using DevInstance.DevCoreApp.Shared.Model.Core.Settings;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Core.Controllers;

[Route("api/settings")]
[Authorize]
public class SettingsController : ApiControllerBase
{
    private readonly ISettingsAdminService _service;

    public SettingsController(ISettingsAdminService service) => _service = service;

    [HttpGet]
    [Authorize(Policy = PermissionDefinitions.Admin.Settings.View)]
    public Task<ActionResult<List<SettingItem>>> GetListAsync([FromQuery] SettingQuery query)
        => HandleServiceAsync(() => _service.GetAllByScopeAsync(query.Scope, query.OrganizationId, query.Search));

    [HttpPost]
    [Authorize(Policy = PermissionDefinitions.Admin.Settings.Edit)]
    public Task<ActionResult<SettingItem>> CreateAsync([FromBody] SettingItem item)
        => HandleServiceAsync(() => _service.CreateSettingAsync(item));

    /// <summary>Body is the new value as a JSON string.</summary>
    [HttpPut("{id}")]
    [Authorize(Policy = PermissionDefinitions.Admin.Settings.Edit)]
    public Task<ActionResult<SettingItem>> UpdateAsync(string id, [FromBody] string value)
        => HandleServiceAsync(() => _service.UpdateSettingAsync(id, value));

    [HttpDelete("{id}")]
    [Authorize(Policy = PermissionDefinitions.Admin.Settings.Edit)]
    public Task<ActionResult<bool>> DeleteAsync(string id)
        => HandleServiceAsync(() => _service.DeleteSettingAsync(id));
}
