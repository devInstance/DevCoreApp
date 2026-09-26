using DevInstance.DevCoreApp.Server.Admin.Services.Core;
using DevInstance.DevCoreApp.Shared.Model.Core.Settings;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Core.Controllers;

/// <summary>The signed-in user's saved grid layouts (global ones need Owner/Admin/Manager to save).</summary>
[Route("api/grid-profiles")]
[Authorize]
public class GridProfilesController : ApiControllerBase
{
    private readonly IGridProfileService _service;

    public GridProfilesController(IGridProfileService service) => _service = service;

    /// <summary>The user's own profile for the grid, else the global one, else null.</summary>
    [HttpGet("{gridName}")]
    public Task<ActionResult<GridProfileItem?>> GetAsync(string gridName, [FromQuery] string profileName = "Default")
        => HandleServiceAsync(() => _service.GetAsync(gridName, profileName));

    [HttpPut]
    public Task<ActionResult<GridProfileItem>> SaveAsync([FromBody] GridProfileItem item)
        => HandleServiceAsync(() => _service.SaveAsync(item));
}
