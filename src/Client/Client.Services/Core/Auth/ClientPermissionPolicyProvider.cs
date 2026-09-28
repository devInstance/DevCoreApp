using DevInstance.DevCoreApp.Shared.Model.Core.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace DevInstance.DevCoreApp.Client.Services.Core.Auth;

/// <summary>
/// Client twin of the server's PermissionPolicyProvider: any policy named like a permission key
/// (<c>Module.Entity.Action</c>) requires that <see cref="PermissionClaims.Type"/> claim. No policy
/// registration per permission, on either side.
/// </summary>
public sealed class ClientPermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public ClientPermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = await base.GetPolicyAsync(policyName);
        if (policy != null || !policyName.Contains('.'))
        {
            return policy;
        }

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim(PermissionClaims.Type, policyName)
            .Build();
    }
}
