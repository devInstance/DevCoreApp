namespace DevInstance.DevCoreApp.Shared.Model.Core.Permissions;

/// <summary>
/// Claim type carrying one permission key (<c>Module.Entity.Action</c>). The server adds these in
/// PermissionClaimsTransformation; the WASM clients add them from <c>GET api/me</c>. On both sides
/// a policy named after a permission key requires a claim of this type with that value.
/// </summary>
public static class PermissionClaims
{
    public const string Type = "Permission";
}
