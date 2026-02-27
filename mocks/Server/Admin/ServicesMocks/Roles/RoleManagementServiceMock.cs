using Bogus;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Admin.Services.Roles;
using DevInstance.DevCoreApp.Shared.Model.Permissions;
using DevInstance.DevCoreApp.Shared.Model.Roles;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Mocks.Roles;

[BlazorServiceMock]
public class RoleManagementServiceMock : IRoleManagementService
{
    private readonly List<RoleItem> roleList;
    private readonly List<PermissionItem> allPermissions;
    private readonly Dictionary<string, HashSet<string>> rolePermissions;
    private readonly int delay = 500;

    public RoleManagementServiceMock()
    {
        // Build permission catalog from PermissionDefinitions
        var allKeys = PermissionDefinitions.GetAll();
        var order = 0;
        allPermissions = allKeys.Select(key =>
        {
            var parts = key.Split('.');
            return new PermissionItem
            {
                Id = IdGenerator.New(),
                Module = parts[0],
                Entity = parts[1],
                Action = parts[2],
                Key = key,
                DisplayOrder = order++
            };
        }).ToList();

        var allPermissionKeys = allPermissions.Select(p => p.Key).ToHashSet();
        var viewOnlyKeys = allPermissions.Where(p => p.Action == "View").Select(p => p.Key).ToHashSet();

        // System roles
        roleList = new List<RoleItem>
        {
            new() { Id = IdGenerator.New(), Name = ApplicationRoles.Owner, Description = "Super-admin with all permissions", IsSystemRole = true, PermissionCount = allPermissions.Count },
            new() { Id = IdGenerator.New(), Name = ApplicationRoles.Admin, Description = "Full administrative access to all features", IsSystemRole = true, PermissionCount = allPermissions.Count },
            new() { Id = IdGenerator.New(), Name = ApplicationRoles.Manager, Description = "Team management and reporting", IsSystemRole = false, PermissionCount = 8 },
            new() { Id = IdGenerator.New(), Name = ApplicationRoles.Employee, Description = "Standard employee access", IsSystemRole = false, PermissionCount = 5 },
            new() { Id = IdGenerator.New(), Name = ApplicationRoles.Client, Description = "Basic read-only access", IsSystemRole = true, PermissionCount = viewOnlyKeys.Count },
        };

        // Role permission mappings
        rolePermissions = new Dictionary<string, HashSet<string>>
        {
            [roleList[0].Id] = allPermissionKeys,
            [roleList[1].Id] = allPermissionKeys,
            [roleList[2].Id] = allPermissions.Where(p => p.Module == "Admin" && (p.Action == "View" || p.Action == "Edit")).Select(p => p.Key).ToHashSet(),
            [roleList[3].Id] = viewOnlyKeys,
            [roleList[4].Id] = viewOnlyKeys,
        };
    }

    public async Task<ServiceActionResult<ModelList<RoleItem>>> GetRolesAsync(int? top, int? page, string[]? sortBy, string? search)
    {
        var pageVal = page ?? 0;
        var topVal = top ?? 10;

        IEnumerable<RoleItem> query = roleList;

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(r =>
                r.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (r.Description != null && r.Description.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        var filtered = query.ToList();
        var items = filtered.Skip(pageVal * topVal).Take(topVal).ToArray();

        await Task.Delay(delay);

        return ServiceActionResult<ModelList<RoleItem>>.OK(
            ModelListResult.CreateList(items, filtered.Count, topVal, pageVal, sortBy, search));
    }

    public async Task<ServiceActionResult<RoleItem>> GetRoleAsync(string roleId)
    {
        var item = roleList.Find(r => r.Id == roleId);
        if (item == null)
            throw new InvalidOperationException("Role not found.");

        await Task.Delay(delay);

        return ServiceActionResult<RoleItem>.OK(item);
    }

    public async Task<ServiceActionResult<RoleItem>> CreateRoleAsync(RoleItem item)
    {
        if (roleList.Any(r => r.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A role with name '{item.Name}' already exists.");

        item.Id = IdGenerator.New();
        item.IsSystemRole = false;
        item.PermissionCount = 0;
        roleList.Add(item);
        rolePermissions[item.Id] = new HashSet<string>();

        await Task.Delay(delay);

        return ServiceActionResult<RoleItem>.OK(item);
    }

    public async Task<ServiceActionResult<RoleItem>> UpdateRoleAsync(string roleId, RoleItem item)
    {
        var index = roleList.FindIndex(r => r.Id == roleId);
        if (index < 0)
            throw new InvalidOperationException("Role not found.");

        var existing = roleList[index];
        if (!existing.IsSystemRole)
        {
            existing.Name = item.Name;
        }
        existing.Description = item.Description;

        await Task.Delay(delay);

        return ServiceActionResult<RoleItem>.OK(existing);
    }

    public async Task<ServiceActionResult<bool>> DeleteRoleAsync(string roleId)
    {
        var item = roleList.Find(r => r.Id == roleId);
        if (item == null)
            throw new InvalidOperationException("Role not found.");

        if (item.IsSystemRole)
            throw new InvalidOperationException("System roles cannot be deleted.");

        roleList.Remove(item);
        rolePermissions.Remove(roleId);

        await Task.Delay(delay);

        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<List<PermissionItem>>> GetAllPermissionsAsync()
    {
        await Task.Delay(delay);

        return ServiceActionResult<List<PermissionItem>>.OK(allPermissions);
    }

    public async Task<ServiceActionResult<List<string>>> GetRolePermissionKeysAsync(string roleId)
    {
        await Task.Delay(delay);

        if (rolePermissions.TryGetValue(roleId, out var keys))
            return ServiceActionResult<List<string>>.OK(keys.ToList());

        return ServiceActionResult<List<string>>.OK(new List<string>());
    }

    public async Task<ServiceActionResult<bool>> SetRolePermissionsAsync(string roleId, RolePermissionsRequest request)
    {
        var role = roleList.Find(r => r.Id == roleId);
        if (role == null)
            throw new InvalidOperationException("Role not found.");

        if (role.IsSystemRole)
            throw new InvalidOperationException("Permissions for system roles are managed automatically.");

        rolePermissions[roleId] = new HashSet<string>(request.PermissionKeys);
        role.PermissionCount = request.PermissionKeys.Count;

        await Task.Delay(delay);

        return ServiceActionResult<bool>.OK(true);
    }
}
