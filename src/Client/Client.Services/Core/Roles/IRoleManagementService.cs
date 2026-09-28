using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Roles;

namespace DevInstance.DevCoreApp.Client.Services.Core.Roles;

/// <summary>Roles and their permissions (<c>api/roles</c>, <c>api/permissions</c>). Same shape as the server service.</summary>
public interface IRoleManagementService
{
    Task<ServiceActionResult<PagedList<RoleItem>>> GetRolesAsync(int? top, int? page, string[]? sortBy, string? search);
    Task<ServiceActionResult<RoleItem>> GetRoleAsync(string roleId);
    Task<ServiceActionResult<RoleItem>> CreateRoleAsync(RoleItem item);
    Task<ServiceActionResult<RoleItem>> UpdateRoleAsync(string roleId, RoleItem item);
    Task<ServiceActionResult<bool>> DeleteRoleAsync(string roleId);
    Task<ServiceActionResult<List<PermissionItem>>> GetAllPermissionsAsync();
    Task<ServiceActionResult<List<string>>> GetRolePermissionKeysAsync(string roleId);
    Task<ServiceActionResult<bool>> SetRolePermissionsAsync(string roleId, RolePermissionsRequest request);
}
