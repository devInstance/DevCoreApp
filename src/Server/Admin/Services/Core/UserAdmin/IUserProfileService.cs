
using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core;
using DevInstance.DevCoreApp.Shared.Model.Core.UserAdmin;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Core.UserAdmin;

public interface IUserProfileService : ICRUDService<UserProfileItem>
{
    ServiceActionResult<UserProfileItem> GetCurrentUser();

    Task<ServiceActionResult<UserProfileItem>> UpdateCurrentUserAsync(UserProfileItem newProfile);

    ServiceActionResult<List<string>> GetAvailableRoles();
    Task<ServiceActionResult<UserProfileItem>> CreateUserAsync(UserProfileItem newUser, string role);

    /// <summary>
    /// Creates a user in a named organization. Pass null for <paramref name="organizationPublicId"/>
    /// to use the creating administrator's primary organization. Every new user gets exactly one
    /// assignment either way — an unassigned user reads every organization and can write to none.
    /// </summary>
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
    Task<ServiceActionResult<(byte[] Data, string ContentType)>> GetProfilePictureAsync(string userId);
    Task<ServiceActionResult<(byte[] Data, string ContentType)>> GetProfilePictureThumbnailAsync(string userId);
}
