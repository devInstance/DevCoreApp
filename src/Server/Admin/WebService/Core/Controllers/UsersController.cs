using DevInstance.DevCoreApp.Server.Admin.Services.Core.UserAdmin;
using DevInstance.DevCoreApp.Shared.Model.Core;
using DevInstance.DevCoreApp.Shared.Model.Core.UserAdmin;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Core.Controllers;

/// <summary>
/// User administration. Profile pictures are <c>api/users/{id}/profile-picture</c>
/// (<see cref="ProfilePictureController"/>).
/// </summary>
[Route("api/users")]
[Authorize]
public class UsersController : ApiControllerBase
{
    private readonly IUserProfileService _service;

    public UsersController(IUserProfileService service) => _service = service;

    [HttpGet]
    [Authorize(Policy = PermissionDefinitions.Admin.Users.View)]
    public Task<ActionResult<PagedList<UserProfileItem>>> GetListAsync([FromQuery] ListQuery query)
        => HandleServiceAsync(() => _service.GetListAsync(query.Top, query.Page, query.SortBy, query.Search));

    /// <summary>Roles an administrator may assign (Owner excluded).</summary>
    [HttpGet("roles")]
    [Authorize(Policy = PermissionDefinitions.Admin.Users.View)]
    public ActionResult<List<string>> GetAvailableRoles()
        => HandleService(() => _service.GetAvailableRoles());

    [HttpGet("{id}")]
    [Authorize(Policy = PermissionDefinitions.Admin.Users.View)]
    public Task<ActionResult<UserProfileItem>> GetAsync(string id)
        => HandleServiceAsync(() => _service.GetAsync(id));

    [HttpPost]
    [Authorize(Policy = PermissionDefinitions.Admin.Users.Create)]
    public Task<ActionResult<UserProfileItem>> CreateAsync([FromBody] CreateUserRequest request)
        => HandleServiceAsync(() => _service.CreateUserAsync(request.User, request.Role, request.OrganizationId));

    [HttpPut("{id}")]
    [Authorize(Policy = PermissionDefinitions.Admin.Users.Edit)]
    public Task<ActionResult<UserProfileItem>> UpdateAsync(string id, [FromBody] UpdateUserRequest request)
        => HandleServiceAsync(() => _service.UpdateUserAsync(id, request.User, request.Role));

    [HttpDelete("{id}")]
    [Authorize(Policy = PermissionDefinitions.Admin.Users.Delete)]
    public Task<ActionResult<bool>> DeleteAsync(string id)
        => HandleServiceAsync(() => _service.DeleteUserAsync(id));

    [HttpGet("{id}/organizations")]
    [Authorize(Policy = PermissionDefinitions.Admin.Users.View)]
    public Task<ActionResult<List<UserOrganizationItem>>> GetOrganizationsAsync(string id)
        => HandleServiceAsync(() => _service.GetUserOrganizationsAsync(id));

    [HttpPut("{id}/organizations")]
    [Authorize(Policy = PermissionDefinitions.Admin.Users.Edit)]
    public Task<ActionResult<bool>> SetOrganizationsAsync(string id, [FromBody] List<UserOrganizationItem> organizations)
        => HandleServiceAsync(() => _service.SetUserOrganizationsAsync(id, organizations));

    [HttpGet("{id}/permission-overrides")]
    [Authorize(Policy = PermissionDefinitions.Admin.Users.View)]
    public Task<ActionResult<List<PermissionOverrideItem>>> GetPermissionOverridesAsync(string id)
        => HandleServiceAsync(() => _service.GetUserPermissionOverridesAsync(id));

    [HttpPut("{id}/permission-overrides")]
    [Authorize(Policy = PermissionDefinitions.Admin.Users.Edit)]
    public Task<ActionResult<bool>> SetPermissionOverridesAsync(string id, [FromBody] List<PermissionOverrideItem> overrides)
        => HandleServiceAsync(() => _service.SetUserPermissionOverridesAsync(id, overrides));

    [HttpGet("{id}/effective-permissions")]
    [Authorize(Policy = PermissionDefinitions.Admin.Users.View)]
    public Task<ActionResult<List<EffectivePermissionItem>>> GetEffectivePermissionsAsync(string id)
        => HandleServiceAsync(() => _service.GetEffectivePermissionsAsync(id));

    [HttpGet("{id}/access-state")]
    [Authorize(Policy = PermissionDefinitions.Admin.Users.View)]
    public Task<ActionResult<UserAccessStateItem>> GetAccessStateAsync(string id)
        => HandleServiceAsync(() => _service.GetUserAccessStateAsync(id));

    [HttpPost("{id}/resend-invitation")]
    [Authorize(Policy = PermissionDefinitions.Admin.Users.Edit)]
    public Task<ActionResult<bool>> ResendInvitationAsync(string id)
        => HandleServiceAsync(() => _service.ResendInvitationAsync(id));

    [HttpPut("{id}/password")]
    [Authorize(Policy = PermissionDefinitions.Admin.Users.Edit)]
    public Task<ActionResult<bool>> SetPasswordAsync(string id, [FromBody] SetUserPasswordRequest request)
        => HandleServiceAsync(() => _service.SetUserPasswordAsync(id, request.Password));
}
