using DevInstance.DevCoreApp.Server.Admin.WebService.Services;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.WebServiceToolkit.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static DevInstance.WebServiceToolkit.Controllers.ControllerUtils;

namespace DevInstance.DevCoreApp.Server.Controllers;

[Route("api/user/profile")]
[ApiController]
public class UserProfileController : ControllerBase
{
    public UserProfileService Service{ get; }

    public UserProfileController(UserProfileService service)
    {
        Service = service;
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UserProfileItem> GetProfile()
    {
        return this.HandleWebRequest((WebHandler<UserProfileItem>)(() =>
        {
            return Ok(Service.GetCurrentUser());
        }));
    }

    [Authorize]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileItem>> UpdateProfileAsync([FromBody] UserProfileItem newProfile)
    {
        return await this.HandleWebRequestAsync<UserProfileItem>(async () =>
        {
            return Ok(await Service.UpdateCurrentUserAsync(newProfile));
        });
    }
}
