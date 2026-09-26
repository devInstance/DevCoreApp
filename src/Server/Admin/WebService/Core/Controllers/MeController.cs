using DevInstance.DevCoreApp.Server.Admin.Services.Core.Appearance;
using DevInstance.DevCoreApp.Server.Admin.Services.Core.UserAdmin;
using DevInstance.DevCoreApp.Shared.Model.Core;
using DevInstance.DevCoreApp.Shared.Model.Core.UserAdmin;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Core.Controllers;

/// <summary>
/// The signed-in user: identity/permissions for the client shell, own profile, and preferences.
/// </summary>
[Route("api/me")]
[Authorize]
public class MeController : ApiControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserProfileService _profiles;
    private readonly IThemeService _theme;

    public MeController(ICurrentUserService currentUser, IUserProfileService profiles, IThemeService theme)
    {
        _currentUser = currentUser;
        _profiles = profiles;
        _theme = theme;
    }

    [HttpGet]
    public Task<ActionResult<CurrentUserItem>> GetAsync()
        => HandleServiceAsync(() => _currentUser.GetAsync());

    [HttpGet("profile")]
    public ActionResult<UserProfileItem> GetProfile()
        => HandleService(() => _profiles.GetCurrentUser());

    [HttpPut("profile")]
    public Task<ActionResult<UserProfileItem>> UpdateProfileAsync([FromBody] UserProfileItem profile)
        => HandleServiceAsync(() => _profiles.UpdateCurrentUserAsync(profile));

    [HttpGet("theme")]
    public Task<ActionResult<string>> GetThemeAsync()
        => HandleServiceAsync(() => _theme.GetUserThemeAsync());

    /// <summary>Body is a JSON string: "Light", "Dark" or "System".</summary>
    [HttpPut("theme")]
    public Task<ActionResult<string>> SetThemeAsync([FromBody] string theme)
        => HandleServiceAsync(() => _theme.SetUserThemeAsync(theme));
}
