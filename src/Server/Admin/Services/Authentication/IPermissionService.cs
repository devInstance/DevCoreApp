using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Authentication;

/// <summary>
/// Provides programmatic permission checks for service-layer and UI logic
/// where [Authorize] attributes don't apply (e.g., conditionally stripping
/// sensitive fields from a response, showing/hiding UI elements).
///
/// Reads from the current ClaimsPrincipal — permissions must have been
/// loaded by PermissionClaimsTransformation before these methods are called.
/// </summary>
public interface IPermissionService
{
    Task<bool> HasPermissionAsync(string permissionKey);
    Task<bool> HasAnyPermissionAsync(params string[] permissionKeys);
    Task<IReadOnlySet<string>> GetEffectivePermissionsAsync();
}
