using DevInstance.DevCoreApp.Server.Admin.Services.Core.UserAdmin;
using DevInstance.DevCoreApp.Shared.Model.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Core.Controllers;

[Route("api/users/{userId}/profile-picture")]
[Authorize]
public class ProfilePictureController : ApiControllerBase
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
        // IFormFile is an HTTP type services must not see; the stream stays open for the call.
        await using var stream = file.OpenReadStream();
        return await HandleServiceAsync(() => _userService.UploadProfilePictureAsync(userId, stream, file.ContentType));
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<bool>> DeleteAsync(string userId)
    {
        return HandleServiceAsync(() => _userService.DeleteProfilePictureAsync(userId));
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
