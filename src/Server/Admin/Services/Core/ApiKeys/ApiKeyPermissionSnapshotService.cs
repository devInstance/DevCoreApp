using DevInstance.DevCoreApp.Server.Database.Core.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Core.ApiKeys;

public interface IApiKeyPermissionSnapshotService
{
    Task<List<string>> GetEffectivePermissionsAsync(Guid userProfileId, CancellationToken cancellationToken = default);
}

public class ApiKeyPermissionSnapshotService : IApiKeyPermissionSnapshotService
{
    private readonly IQueryRepository _repository;

    public ApiKeyPermissionSnapshotService(IQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<string>> GetEffectivePermissionsAsync(Guid userProfileId, CancellationToken cancellationToken = default)
    {
        var applicationUserId = await _repository.GetUserProfilesQuery(null!)
            .ById(userProfileId)
            .Select()
            .Select(up => (Guid?)up.ApplicationUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!applicationUserId.HasValue)
            return new List<string>();

        var roleIds = await _repository.GetRolePermissionQuery(null!)
            .GetRoleIdsForUserAsync(applicationUserId.Value);

        var permissionKeys = new HashSet<string>(StringComparer.Ordinal);

        if (roleIds.Count > 0)
        {
            var rolePermissions = await _repository.GetRolePermissionQuery(null!)
                .GetPermissionKeysForRoleIdsAsync(roleIds);

            foreach (var key in rolePermissions)
                permissionKeys.Add(key);
        }

        // Note: overrides are keyed by userProfileId here (preserves existing behavior).
        var overrides = await _repository.GetUserPermissionOverrideQuery(null!)
            .ByUserId(userProfileId)
            .IncludePermission()
            .Select()
            .ToListAsync(cancellationToken);

        foreach (var ov in overrides)
        {
            if (ov.IsGranted)
                permissionKeys.Add(ov.Permission!.Key);
            else
                permissionKeys.Remove(ov.Permission!.Key);
        }

        return permissionKeys
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
    }
}
