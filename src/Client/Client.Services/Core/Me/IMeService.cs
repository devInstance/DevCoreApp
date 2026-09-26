using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core;
using DevInstance.DevCoreApp.Shared.Model.Core.UserAdmin;

namespace DevInstance.DevCoreApp.Client.Services.Core.Me;

/// <summary>The signed-in user (<c>api/me</c>).</summary>
public interface IMeService
{
    /// <summary>Profile, roles, effective permissions and theme.</summary>
    Task<ServiceActionResult<CurrentUserItem>> GetAsync();

    Task<ServiceActionResult<UserProfileItem>> UpdateProfileAsync(UserProfileItem profile);

    /// <summary>"Light", "Dark" or "System".</summary>
    Task<ServiceActionResult<string>> SetThemeAsync(string theme);
}
