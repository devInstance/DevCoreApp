using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Roles;
using DevInstance.WebServiceToolkit.Common.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Roles;

public interface IRoleManagementService
{
    Task<ServiceActionResult<ModelList<RoleItem>>> GetRolesAsync(int? top, int? page, string[]? sortBy, string? search);
    Task<ServiceActionResult<RoleItem>> GetRoleAsync(string roleId);
    Task<ServiceActionResult<RoleItem>> CreateRoleAsync(RoleItem item);
    Task<ServiceActionResult<RoleItem>> UpdateRoleAsync(string roleId, RoleItem item);
    Task<ServiceActionResult<bool>> DeleteRoleAsync(string roleId);
    Task<ServiceActionResult<List<PermissionItem>>> GetAllPermissionsAsync();
    Task<ServiceActionResult<List<string>>> GetRolePermissionKeysAsync(string roleId);
    Task<ServiceActionResult<bool>> SetRolePermissionsAsync(string roleId, RolePermissionsRequest request);
}
