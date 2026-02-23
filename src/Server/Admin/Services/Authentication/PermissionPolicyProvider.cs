using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Authentication;

/// <summary>
/// Dynamically creates authorization policies for permission-based checks.
///
/// When a controller or page uses [Authorize(Policy = "Admin.Users.View")],
/// ASP.NET Core asks the policy provider for a policy with that name.
/// If the name contains a dot (the Module.Entity.Action format), this provider
/// builds a policy requiring a claim of type "Permission" with that exact value.
///
/// Non-permission policy names (no dot) fall through to the default provider,
/// which resolves policies registered via AddAuthorization().
///
/// Registered as singleton because it is stateless — the claim requirement
/// is built from the policy name alone, with no database access.
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallback.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallback.GetFallbackPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.Contains('.'))
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireClaim(PermissionClaimsTransformation.PermissionClaimType, policyName)
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
