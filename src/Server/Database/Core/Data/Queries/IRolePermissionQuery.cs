using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

/// <summary>
/// A single role-grants-permission pairing, carrying the granting role's name.
/// Used to explain WHY a permission is effective (e.g. the admin "effective permissions" screen).
/// </summary>
public record RolePermissionGrant(Guid PermissionId, string? RoleName);

/// <summary>
/// Cohesive query for the role/permission mapping tables that have no single owning entity:
/// RolePermission (composite key, no Id) plus the ASP.NET Identity Roles/UserRoles reads that
/// permission resolution depends on. This is intentionally NOT an IModelQuery — these are
/// materializing reads and bulk mutations, never an exposed IQueryable.
/// </summary>
public interface IRolePermissionQuery
{
    Task<int> CountForRoleIdAsync(Guid roleId);
    Task<IReadOnlyDictionary<Guid, int>> CountByRoleIdsAsync(IReadOnlyList<Guid> roleIds);
    Task<bool> RoleHasUsersAsync(Guid roleId);

    Task<IReadOnlyList<Guid>> GetRoleIdsByNamesAsync(IReadOnlyList<string> roleNames);
    Task<IReadOnlyList<Guid>> GetRoleIdsForUserAsync(Guid applicationUserId);

    Task<IReadOnlyList<string>> GetPermissionKeysForRoleIdAsync(Guid roleId);
    Task<IReadOnlyList<string>> GetPermissionKeysForRoleIdsAsync(IReadOnlyList<Guid> roleIds);
    Task<IReadOnlyList<string>> GetPermissionKeysForRoleNamesAsync(IReadOnlyList<string> roleNames);

    Task<IReadOnlyList<RolePermissionGrant>> GetRolePermissionGrantsForRoleIdsAsync(IReadOnlyList<Guid> roleIds);

    /// <summary>
    /// Full replace of a role's permission mappings: removes existing rows, then adds one row
    /// per supplied permission key that resolves to a known permission, in a single save.
    /// </summary>
    Task ReplaceRolePermissionsAsync(Guid roleId, IReadOnlyList<string> permissionKeys);

    /// <summary>Removes all permission mappings for the role in a single save.</summary>
    Task RemoveAllForRoleAsync(Guid roleId);
}
