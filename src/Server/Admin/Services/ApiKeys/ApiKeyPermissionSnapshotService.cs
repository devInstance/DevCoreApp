using DevInstance.DevCoreApp.Server.Database.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.ApiKeys;

public interface IApiKeyPermissionSnapshotService
{
    Task<List<string>> GetEffectivePermissionsAsync(Guid userProfileId, CancellationToken cancellationToken = default);
}

public class ApiKeyPermissionSnapshotService : IApiKeyPermissionSnapshotService
{
    private readonly ApplicationDbContext _db;

    public ApiKeyPermissionSnapshotService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<string>> GetEffectivePermissionsAsync(Guid userProfileId, CancellationToken cancellationToken = default)
    {
        var applicationUserId = await _db.UserProfiles
            .Where(up => up.Id == userProfileId)
            .Select(up => (Guid?)up.ApplicationUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!applicationUserId.HasValue)
            return new List<string>();

        var roleIds = await _db.UserRoles
            .Where(ur => ur.UserId == applicationUserId.Value)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        var permissionKeys = new HashSet<string>(StringComparer.Ordinal);

        if (roleIds.Count > 0)
        {
            var rolePermissions = await _db.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => rp.Permission!.Key)
                .ToListAsync(cancellationToken);

            foreach (var key in rolePermissions)
                permissionKeys.Add(key);
        }

        var overrides = await _db.UserPermissionOverrides
            .Where(upo => upo.UserId == userProfileId)
            .Select(upo => new { upo.Permission!.Key, upo.IsGranted })
            .ToListAsync(cancellationToken);

        foreach (var ov in overrides)
        {
            if (ov.IsGranted)
                permissionKeys.Add(ov.Key);
            else
                permissionKeys.Remove(ov.Key);
        }

        return permissionKeys
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
    }
}
