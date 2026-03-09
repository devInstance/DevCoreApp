using DevInstance.DevCoreApp.Server.Admin.Services.UserAdmin;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.WebServiceToolkit.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static DevInstance.WebServiceToolkit.Controllers.ControllerUtils;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Controllers;

[Route("api/users/{userId}/profile-picture")]
[ApiController]
[Authorize]
public class ProfilePictureController : ControllerBase
{
    private readonly IUserProfileService _userService;

    public ProfilePictureController(IUserProfileService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileItem>> UploadAsync(string userId, IFormFile file)
    {
        return await this.HandleWebRequestAsync<UserProfileItem>(async () =>
        {
            using var stream = file.OpenReadStream();
            var result = await _userService.UploadProfilePictureAsync(userId, stream, file.ContentType);
            return Ok(result.Result);
        });
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> DeleteAsync(string userId)
    {
        return await this.HandleWebRequestAsync<bool>(async () =>
        {
            var result = await _userService.DeleteProfilePictureAsync(userId);
            return Ok(result.Result);
        });
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> GetAsync(string userId)
    {
        var result = await _userService.GetProfilePictureAsync(userId);
        var (data, contentType) = result.Result;
        return File(data, contentType);
    }

    [HttpGet("thumbnail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> GetThumbnailAsync(string userId)
    {
        var result = await _userService.GetProfilePictureThumbnailAsync(userId);
        var (data, contentType) = result.Result;
        return File(data, contentType);
    }
}
