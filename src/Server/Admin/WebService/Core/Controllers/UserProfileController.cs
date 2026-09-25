using DevInstance.DevCoreApp.Server.Admin.Services.Core.UserAdmin;
using DevInstance.DevCoreApp.Shared.Model.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Core.Controllers;

[Route("api/user/profile")]
[Authorize]
public class UserProfileController : ApiControllerBase
{
    private readonly IUserProfileService _service;

    public UserProfileController(IUserProfileService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UserProfileItem> GetProfile()
    {
        return HandleService(() => _service.GetCurrentUser());
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<UserProfileItem>> UpdateProfileAsync([FromBody] UserProfileItem newProfile)
    {
        return HandleServiceAsync(() => _service.UpdateCurrentUserAsync(newProfile));
    }
}
