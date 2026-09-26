using DevInstance.BlazorToolkit.Http;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Client.Services.Core.Time;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.LogScope;
using DevInstance.DevCoreApp.Shared.Model.Core;
using DevInstance.DevCoreApp.Shared.Model.Core.UserAdmin;

namespace DevInstance.DevCoreApp.Client.Services.Core.Users;

[BlazorService]
public class UserProfileService : ApiServiceBase, IUserProfileService
{
    private readonly IProfilePictureService pictures;

    public UserProfileService(IHttpApiContextFactory apiFactory, IProfilePictureService pictures, IScopeManager logManager)
        : base(apiFactory, logManager)
    {
        this.pictures = pictures;
    }

    private static string User(string id) => $"api/users/{Segment(id)}";

    public Task<ServiceActionResult<PagedList<UserProfileItem>>> GetListAsync(int? top, int? page, string[] sortBy, string search) =>
        CallAsync(() => Api<UserProfileItem>("api/users").Get()
            .Query(new ListQuery { Top = top ?? 20, Page = page ?? 0, SortBy = sortBy, Search = search })
            .ExecuteAsync<PagedList<UserProfileItem>>());

    public Task<ServiceActionResult<UserProfileItem>> GetAsync(string id) =>
        CallAsync(() => Api<UserProfileItem>(User(id)).Get().ExecuteAsync());

    public Task<ServiceActionResult<List<string>>> GetAvailableRolesAsync() =>
        CallAsync(() => Api<List<string>>("api/users/roles").Get().ExecuteAsync());

    public Task<ServiceActionResult<UserProfileItem>> CreateUserAsync(UserProfileItem newUser, string role, string? organizationPublicId) =>
        CallAsync(() => Api<UserProfileItem>("api/users")
            .Post<CreateUserRequest>(new CreateUserRequest { User = newUser, Role = role, OrganizationId = organizationPublicId! })
            .ExecuteAsync());

    public Task<ServiceActionResult<UserProfileItem>> UpdateUserAsync(string id, UserProfileItem updatedUser, string role) =>
        CallAsync(() => Api<UserProfileItem>(User(id))
            .Put<UpdateUserRequest>(new UpdateUserRequest { User = updatedUser, Role = role })
            .ExecuteAsync());

    public Task<ServiceActionResult<bool>> DeleteUserAsync(string id) =>
        CallAsync(() => Api<bool>(User(id)).Delete().ExecuteAsync());

    public Task<ServiceActionResult<List<UserOrganizationItem>>> GetUserOrganizationsAsync(string userId) =>
        CallAsync(() => Api<List<UserOrganizationItem>>(User(userId) + "/organizations").Get().ExecuteAsync());

    public Task<ServiceActionResult<bool>> SetUserOrganizationsAsync(string userId, List<UserOrganizationItem> organizations) =>
        CallAsync(() => Api<bool>(User(userId) + "/organizations").Put<List<UserOrganizationItem>>(organizations).ExecuteAsync());

    public Task<ServiceActionResult<List<PermissionOverrideItem>>> GetUserPermissionOverridesAsync(string userId) =>
        CallAsync(() => Api<List<PermissionOverrideItem>>(User(userId) + "/permission-overrides").Get().ExecuteAsync());

    public Task<ServiceActionResult<bool>> SetUserPermissionOverridesAsync(string userId, List<PermissionOverrideItem> overrides) =>
        CallAsync(() => Api<bool>(User(userId) + "/permission-overrides").Put<List<PermissionOverrideItem>>(overrides).ExecuteAsync());

    public Task<ServiceActionResult<List<EffectivePermissionItem>>> GetEffectivePermissionsAsync(string userId) =>
        CallAsync(() => Api<List<EffectivePermissionItem>>(User(userId) + "/effective-permissions").Get().ExecuteAsync());

    public Task<ServiceActionResult<UserAccessStateItem>> GetUserAccessStateAsync(string userId) =>
        CallAsync(() => Api<UserAccessStateItem>(User(userId) + "/access-state").Get().ExecuteAsync());

    public Task<ServiceActionResult<bool>> ResendInvitationAsync(string userId) =>
        CallAsync(() => Api<bool>(User(userId) + "/resend-invitation").Post<object?>(null).ExecuteAsync());

    public Task<ServiceActionResult<bool>> SetUserPasswordAsync(string userId, string password) =>
        CallAsync(() => Api<bool>(User(userId) + "/password")
            .Put<SetUserPasswordRequest>(new SetUserPasswordRequest { Password = password })
            .ExecuteAsync());

    public Task<ServiceActionResult<UserProfileItem>> UploadProfilePictureAsync(string userId, Stream imageStream, string contentType) =>
        pictures.UploadAsync(userId, imageStream, "picture", contentType);

    public Task<ServiceActionResult<bool>> DeleteProfilePictureAsync(string userId) =>
        pictures.DeleteAsync(userId);
}
