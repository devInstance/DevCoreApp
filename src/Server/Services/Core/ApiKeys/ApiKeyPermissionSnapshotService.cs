using DevInstance.DevCoreApp.Server.Database.Core.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Services.Core.ApiKeys;

public interface IApiKeyPermissionSnapshotService
{
    Task<List<string>> GetEffectivePermissionsAsync(Guid userProfileId, CancellationToken cancellationToken = default);
}

public class ApiKeyPermissionSnapshotService : IApiKeyPermissionSnapshotService
{
    private readonly IQueryRepositoryFactory _repositoryFactory;

    public ApiKeyPermissionSnapshotService(IQueryRepositoryFactory repositoryFactory)
    {
        _repositoryFactory = repositoryFactory;
    }

    public async Task<List<string>> GetEffectivePermissionsAsync(Guid userProfileId, CancellationToken cancellationToken = default)
    {
        // Own unit of work: the caller (ApiKeyAdminService.CreateKeyAsync) is mid-operation on its
        // own context, and must not share the request's scoped one with this read.
        await using var repo = _repositoryFactory.Create();

        var applicationUserId = await repo.GetUserProfilesQuery(null!)
            .ById(userProfileId)
            .Select()
            .Select(up => (Guid?)up.ApplicationUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!applicationUserId.HasValue)
            return new List<string>();

        var roleIds = await repo.GetRolePermissionQuery(null!)
            .GetRoleIdsForUserAsync(applicationUserId.Value);

        var permissionKeys = new HashSet<string>(StringComparer.Ordinal);

        if (roleIds.Count > 0)
        {
            var rolePermissions = await repo.GetRolePermissionQuery(null!)
                .GetPermissionKeysForRoleIdsAsync(roleIds);

            foreach (var key in rolePermissions)
                permissionKeys.Add(key);
        }

        // Overrides are keyed by the Identity user id (UserPermissionOverride.UserId → ApplicationUser),
        // the same key PermissionClaimsTransformation uses — not the profile id.
        var overrides = await repo.GetUserPermissionOverrideQuery(null!)
            .ByUserId(applicationUserId.Value)
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
