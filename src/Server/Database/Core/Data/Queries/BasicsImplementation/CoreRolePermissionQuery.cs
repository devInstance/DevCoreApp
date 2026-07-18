using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public class CoreRolePermissionQuery : CoreBaseQuery, IRolePermissionQuery
{
    public CoreRolePermissionQuery(IScopeManager logManager,
                             ITimeProvider timeProvider,
                             ApplicationDbContext dB,
                             UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public async Task<int> CountForRoleIdAsync(Guid roleId)
    {
        return await DB.RolePermissions.CountAsync(rp => rp.RoleId == roleId);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountByRoleIdsAsync(IReadOnlyList<Guid> roleIds)
    {
        var counts = await DB.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .GroupBy(rp => rp.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count);

        return counts;
    }

    public async Task<bool> RoleHasUsersAsync(Guid roleId)
    {
        return await DB.UserRoles.AnyAsync(ur => ur.RoleId == roleId);
    }

    public async Task<IReadOnlyList<Guid>> GetRoleIdsByNamesAsync(IReadOnlyList<string> roleNames)
    {
        return await DB.Roles
            .Where(r => roleNames.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Guid>> GetRoleIdsForUserAsync(Guid applicationUserId)
    {
        return await DB.UserRoles
            .Where(ur => ur.UserId == applicationUserId)
            .Select(ur => ur.RoleId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetPermissionKeysForRoleIdAsync(Guid roleId)
    {
        return await DB.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Join(DB.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p.Key)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetPermissionKeysForRoleIdsAsync(IReadOnlyList<Guid> roleIds)
    {
        return await DB.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission!.Key)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetPermissionKeysForRoleNamesAsync(IReadOnlyList<string> roleNames)
    {
        return await DB.RolePermissions
            .Where(rp => rp.Role != null && roleNames.Contains(rp.Role.Name!))
            .Select(rp => rp.Permission!.Key)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<RolePermissionGrant>> GetRolePermissionGrantsForRoleIdsAsync(IReadOnlyList<Guid> roleIds)
    {
        var rows = await DB.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Join(DB.Roles, rp => rp.RoleId, r => r.Id, (rp, r) => new { rp.PermissionId, RoleName = r.Name })
            .ToListAsync();

        return rows.Select(x => new RolePermissionGrant(x.PermissionId, x.RoleName)).ToList();
    }

    public async Task ReplaceRolePermissionsAsync(Guid roleId, IReadOnlyList<string> permissionKeys)
    {
        var existing = await DB.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();
        DB.RolePermissions.RemoveRange(existing);

        if (permissionKeys.Count > 0)
        {
            var permissionsByKey = await DB.Permissions
                .Where(p => permissionKeys.Contains(p.Key))
                .ToDictionaryAsync(p => p.Key, p => p.Id);

            foreach (var key in permissionKeys)
            {
                if (permissionsByKey.TryGetValue(key, out var permissionId))
                {
                    DB.RolePermissions.Add(new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permissionId
                    });
                }
            }
        }

        await DB.SaveChangesAsync();
    }

    public async Task RemoveAllForRoleAsync(Guid roleId)
    {
        var rolePermissions = await DB.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();
        DB.RolePermissions.RemoveRange(rolePermissions);
        await DB.SaveChangesAsync();
    }
}
