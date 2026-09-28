using DevInstance.BlazorToolkit.Http;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Client.Services.Core.Time;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.LogScope;
using DevInstance.DevCoreApp.Shared.Model.Core.Roles;

namespace DevInstance.DevCoreApp.Client.Services.Core.Roles;

[BlazorService]
public class RoleManagementService : ApiServiceBase, IRoleManagementService
{
    public RoleManagementService(IHttpApiContextFactory apiFactory, IScopeManager logManager) : base(apiFactory, logManager) { }

    public Task<ServiceActionResult<PagedList<RoleItem>>> GetRolesAsync(int? top, int? page, string[]? sortBy, string? search) =>
        CallAsync(() => Api<RoleItem>("api/roles").Get()
            .Query(new ListQuery { Top = top ?? 20, Page = page ?? 0, SortBy = sortBy!, Search = search! })
            .ExecuteAsync<PagedList<RoleItem>>());

    public Task<ServiceActionResult<RoleItem>> GetRoleAsync(string roleId) =>
        CallAsync(() => Api<RoleItem>($"api/roles/{Segment(roleId)}").Get().ExecuteAsync());

    public Task<ServiceActionResult<RoleItem>> CreateRoleAsync(RoleItem item) =>
        CallAsync(() => Api<RoleItem>("api/roles").Post(item).ExecuteAsync());

    public Task<ServiceActionResult<RoleItem>> UpdateRoleAsync(string roleId, RoleItem item) =>
        CallAsync(() => Api<RoleItem>($"api/roles/{Segment(roleId)}").Put(item).ExecuteAsync());

    public Task<ServiceActionResult<bool>> DeleteRoleAsync(string roleId) =>
        CallAsync(() => Api<bool>($"api/roles/{Segment(roleId)}").Delete().ExecuteAsync());

    public Task<ServiceActionResult<List<PermissionItem>>> GetAllPermissionsAsync() =>
        CallAsync(() => Api<List<PermissionItem>>("api/permissions").Get().ExecuteAsync());

    public Task<ServiceActionResult<List<string>>> GetRolePermissionKeysAsync(string roleId) =>
        CallAsync(() => Api<List<string>>($"api/roles/{Segment(roleId)}/permissions").Get().ExecuteAsync());

    public Task<ServiceActionResult<bool>> SetRolePermissionsAsync(string roleId, RolePermissionsRequest request) =>
        CallAsync(() => Api<bool>($"api/roles/{Segment(roleId)}/permissions").Put<RolePermissionsRequest>(request).ExecuteAsync());
}
