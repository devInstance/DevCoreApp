
using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.DevCoreApp.Shared.Model.UserAdmin;

namespace DevInstance.DevCoreApp.Server.Admin.Services.UserAdmin;

public interface IUserProfileService : ICRUDService<UserProfileItem>
{
    ServiceActionResult<UserProfileItem> GetCurrentUser();

    Task<ServiceActionResult<UserProfileItem>> UpdateCurrentUserAsync(UserProfileItem newProfile);

    ServiceActionResult<List<string>> GetAvailableRoles();
    Task<ServiceActionResult<UserProfileItem>> CreateUserAsync(UserProfileItem newUser, string role);
    Task<ServiceActionResult<UserProfileItem>> UpdateUserAsync(string id, UserProfileItem updatedUser, string role);
    Task<ServiceActionResult<bool>> DeleteUserAsync(string id);

    Task<ServiceActionResult<List<UserOrganizationItem>>> GetUserOrganizationsAsync(string userId);
    Task<ServiceActionResult<bool>> SetUserOrganizationsAsync(string userId, List<UserOrganizationItem> organizations);
    Task<ServiceActionResult<List<PermissionOverrideItem>>> GetUserPermissionOverridesAsync(string userId);
    Task<ServiceActionResult<bool>> SetUserPermissionOverridesAsync(string userId, List<PermissionOverrideItem> overrides);
    Task<ServiceActionResult<List<EffectivePermissionItem>>> GetEffectivePermissionsAsync(string userId);
}
