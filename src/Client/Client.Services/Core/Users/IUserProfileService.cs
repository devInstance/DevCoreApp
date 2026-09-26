using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.UserAdmin;

namespace DevInstance.DevCoreApp.Client.Services.Core.Users;

/// <summary>
/// User administration (<c>api/users</c>). Same shape as the server service, except that the
/// server's synchronous <c>GetAvailableRoles</c> is async here — it is an HTTP call.
/// The signed-in user's own profile is <c>IMeService</c>.
/// </summary>
public interface IUserProfileService
{
    Task<ServiceActionResult<PagedList<UserProfileItem>>> GetListAsync(int? top, int? page, string[] sortBy, string search);
    Task<ServiceActionResult<UserProfileItem>> GetAsync(string id);
    Task<ServiceActionResult<List<string>>> GetAvailableRolesAsync();
    Task<ServiceActionResult<UserProfileItem>> CreateUserAsync(UserProfileItem newUser, string role, string? organizationPublicId);
    Task<ServiceActionResult<UserProfileItem>> UpdateUserAsync(string id, UserProfileItem updatedUser, string role);
    Task<ServiceActionResult<bool>> DeleteUserAsync(string id);
    Task<ServiceActionResult<List<UserOrganizationItem>>> GetUserOrganizationsAsync(string userId);
    Task<ServiceActionResult<bool>> SetUserOrganizationsAsync(string userId, List<UserOrganizationItem> organizations);
    Task<ServiceActionResult<List<PermissionOverrideItem>>> GetUserPermissionOverridesAsync(string userId);
    Task<ServiceActionResult<bool>> SetUserPermissionOverridesAsync(string userId, List<PermissionOverrideItem> overrides);
    Task<ServiceActionResult<List<EffectivePermissionItem>>> GetEffectivePermissionsAsync(string userId);
    Task<ServiceActionResult<UserAccessStateItem>> GetUserAccessStateAsync(string userId);
    Task<ServiceActionResult<bool>> ResendInvitationAsync(string userId);
    Task<ServiceActionResult<bool>> SetUserPasswordAsync(string userId, string password);
    Task<ServiceActionResult<UserProfileItem>> UploadProfilePictureAsync(string userId, Stream imageStream, string contentType);
    Task<ServiceActionResult<bool>> DeleteProfilePictureAsync(string userId);
}
