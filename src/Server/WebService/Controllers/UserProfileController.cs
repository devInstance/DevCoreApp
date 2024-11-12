using DevInstance.DevCoreApp.Server.Services;
using DevInstance.DevCoreApp.Shared.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Controllers
{
    [Route("api/user/profile")]
    [ApiController]
    public class UserProfileController : BaseController
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
            return HandleWebRequest((WebHandler<UserProfileItem>)(() =>
            {
                return Ok(Service.Get());
            }));
        }

        [Authorize]
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserProfileItem>> UpdateProfileAsync([FromBody] UserProfileItem newProfile)
        {
            return await HandleWebRequestAsync<UserProfileItem>(async () =>
            {
                return Ok(await Service.UpdateAsync(newProfile));
            });
        }
    }
}
