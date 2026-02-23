using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Model.Permissions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Seeding;

/// <summary>
/// Seeds Permission records from PermissionDefinitions, ensures system roles
/// (Admin, User) exist with IsSystemRole = true, and syncs their
/// role-permission mappings. Idempotent — safe to run on every startup.
///
/// Runs after OrganizationDataSeeder (Order = 20 vs 10) so the database
/// has basic infrastructure in place first.
/// </summary>
public class PermissionSeeder : IDataSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public int Order => 20;

    public PermissionSeeder(ApplicationDbContext db, RoleManager<ApplicationRole> roleManager)
    {
        _db = db;
        _roleManager = roleManager;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // 1. Seed all permission records from PermissionDefinitions
        var allKeys = PermissionDefinitions.GetAll();
        var existingPermissions = await _db.Permissions
            .ToDictionaryAsync(p => p.Key, cancellationToken);

        var displayOrder = existingPermissions.Count > 0
            ? existingPermissions.Values.Max(p => p.DisplayOrder) + 1
            : 0;

        foreach (var key in allKeys)
        {
            if (existingPermissions.ContainsKey(key))
                continue;

            var parts = key.Split('.');
            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Module = parts[0],
                Entity = parts[1],
                Action = parts[2],
                Key = key,
                DisplayOrder = displayOrder++
            };

            _db.Permissions.Add(permission);
            existingPermissions[key] = permission;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // 2. Ensure system roles exist
        await EnsureSystemRoleAsync("Admin", "Full administrative access to all features");
        await EnsureSystemRoleAsync("User", "Basic read-only access");

        // 3. Sync role-permission mappings for system roles
        // Reload permissions after save to get stable IDs
        var permissionsByKey = await _db.Permissions
            .ToDictionaryAsync(p => p.Key, cancellationToken);

        await SyncRolePermissionsAsync("Admin", allKeys, permissionsByKey, cancellationToken);

        var userPermissionKeys = allKeys
            .Where(k => k.EndsWith(".View"))
            .ToList();
        await SyncRolePermissionsAsync("User", userPermissionKeys, permissionsByKey, cancellationToken);
    }

    private async Task EnsureSystemRoleAsync(string roleName, string description)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            await _roleManager.CreateAsync(new ApplicationRole(roleName)
            {
                IsSystemRole = true,
                Description = description
            });
        }
        else if (!role.IsSystemRole)
        {
            role.IsSystemRole = true;
            role.Description ??= description;
            await _roleManager.UpdateAsync(role);
        }
    }

    private async Task SyncRolePermissionsAsync(
        string roleName,
        IReadOnlyList<string> desiredKeys,
        Dictionary<string, Permission> permissionsByKey,
        CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
            return;

        var existingMappings = await _db.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .ToListAsync(cancellationToken);

        var existingPermissionIds = existingMappings
            .Select(rp => rp.PermissionId)
            .ToHashSet();

        var desiredPermissionIds = desiredKeys
            .Where(k => permissionsByKey.ContainsKey(k))
            .Select(k => permissionsByKey[k].Id)
            .ToHashSet();

        // Add missing mappings
        foreach (var permissionId in desiredPermissionIds)
        {
            if (!existingPermissionIds.Contains(permissionId))
            {
                _db.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permissionId
                });
            }
        }

        // Remove stale mappings (only for system roles)
        foreach (var mapping in existingMappings)
        {
            if (!desiredPermissionIds.Contains(mapping.PermissionId))
            {
                _db.RolePermissions.Remove(mapping);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
