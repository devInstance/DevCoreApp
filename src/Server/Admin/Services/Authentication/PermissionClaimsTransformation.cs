using DevInstance.DevCoreApp.Server.Database.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Authentication;

/// <summary>
/// Transforms the ClaimsPrincipal on each authenticated request to inject
/// permission claims resolved from the database.
///
/// Flow:
///   1. Read role claims already present on the principal (from Identity cookie)
///   2. Query RolePermissions to collect all permission keys for those roles
///   3. Query UserPermissionOverrides to apply per-user grants and denials
///   4. Add Permission:{key} claims for each effective permission
///   5. Resolve the user's organization context and add org claims
///   6. Stamp a PermissionsLoaded sentinel claim to skip on subsequent calls
///
/// Registered as IClaimsTransformation — ASP.NET Core calls TransformAsync
/// on every authentication. The sentinel claim prevents repeated DB queries
/// within the same request scope.
/// </summary>
public class PermissionClaimsTransformation : IClaimsTransformation
{
    public const string PermissionClaimType = "Permission";
    public const string PermissionsLoadedClaimType = "PermissionsLoaded";
    public const string VisibleOrganizationClaimType = "VisibleOrganization";
    public const string PrimaryOrganizationClaimType = "PrimaryOrganization";

    private readonly ApplicationDbContext _db;
    private readonly IOrganizationContextResolver _orgResolver;

    public PermissionClaimsTransformation(
        ApplicationDbContext db,
        IOrganizationContextResolver orgResolver)
    {
        _db = db;
        _orgResolver = orgResolver;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return principal;

        // Skip if already transformed in this request
        if (principal.HasClaim(c => c.Type == PermissionsLoadedClaimType))
            return principal;

        var userIdString = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            return principal;

        // 1. Collect role names from existing claims
        var roleNames = identity.Claims
            .Where(c => c.Type == identity.RoleClaimType)
            .Select(c => c.Value)
            .ToList();

        // 2. Resolve permission keys from role-permission mappings
        var permissionKeys = new HashSet<string>();

        if (roleNames.Count > 0)
        {
            var rolePermissionKeys = await _db.RolePermissions
                .Where(rp => rp.Role != null && roleNames.Contains(rp.Role.Name!))
                .Select(rp => rp.Permission!.Key)
                .ToListAsync();

            foreach (var key in rolePermissionKeys)
            {
                permissionKeys.Add(key);
            }
        }

        // 3. Apply user-level overrides (grants and denials)
        var overrides = await _db.UserPermissionOverrides
            .Where(upo => upo.UserId == userId)
            .Select(upo => new { upo.Permission!.Key, upo.IsGranted })
            .ToListAsync();

        foreach (var ov in overrides)
        {
            if (ov.IsGranted)
                permissionKeys.Add(ov.Key);
            else
                permissionKeys.Remove(ov.Key);
        }

        // 4. Add permission claims
        foreach (var key in permissionKeys)
        {
            identity.AddClaim(new Claim(PermissionClaimType, key));
        }

        // 4b. API key scope restriction — if this principal was authenticated via an API key,
        //     intersect the full permission set with the key's allowed scopes.
        var apiKeyIdClaim = identity.FindFirst("ApiKeyId")?.Value;
        if (!string.IsNullOrEmpty(apiKeyIdClaim))
        {
            var apiKey = await _db.ApiKeys
                .Where(ak => ak.PublicId == apiKeyIdClaim)
                .Select(ak => new { ak.Scopes })
                .FirstOrDefaultAsync();

            if (apiKey?.Scopes != null && apiKey.Scopes.Count > 0)
            {
                var scopeSet = new HashSet<string>(apiKey.Scopes);
                var toRemove = identity.Claims
                    .Where(c => c.Type == PermissionClaimType && !scopeSet.Contains(c.Value))
                    .ToList();
                foreach (var claim in toRemove)
                    identity.RemoveClaim(claim);
            }
        }

        // 5. Resolve organization context
        var orgContext = await _orgResolver.ResolveAsync(userId);

        if (orgContext.PrimaryOrganizationId.HasValue)
        {
            identity.AddClaim(new Claim(
                PrimaryOrganizationClaimType,
                orgContext.PrimaryOrganizationId.Value.ToString()));
        }

        foreach (var orgId in orgContext.VisibleOrganizationIds)
        {
            identity.AddClaim(new Claim(
                VisibleOrganizationClaimType,
                orgId.ToString()));
        }

        // 6. Mark as loaded
        identity.AddClaim(new Claim(PermissionsLoadedClaimType, "true"));

        return principal;
    }
}
