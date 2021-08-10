using DevInstance.SampleWebApp.Server.Services;
using DevInstance.SampleWebApp.Shared.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.SampleWebApp.Server.Controllers
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
        public ActionResult<UserProfileItem> UpdateProfile([FromBody] UserProfileItem newProfile)
        {
            return HandleWebRequest((WebHandler<UserProfileItem>)(() =>
            {
                return Ok(Service.Update(newProfile));
            }));
        }
    }
}
