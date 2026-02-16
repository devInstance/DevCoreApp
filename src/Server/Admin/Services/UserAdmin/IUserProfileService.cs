
using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model;

namespace DevInstance.DevCoreApp.Server.Admin.Services.UserAdmin;

public interface IUserProfileService : ICRUDService<UserProfileItem>
{
    ServiceActionResult<UserProfileItem> GetCurrentUser();

    Task<ServiceActionResult<UserProfileItem>> UpdateCurrentUserAsync(UserProfileItem newProfile);

    ServiceActionResult<List<string>> GetAvailableRoles();
    Task<ServiceActionResult<UserProfileItem>> CreateUserAsync(UserProfileItem newUser, string role);
    Task<ServiceActionResult<UserProfileItem>> UpdateUserAsync(string id, UserProfileItem updatedUser, string role);
    Task<ServiceActionResult<bool>> DeleteUserAsync(string id);
}
