using System.Security.Claims;
using DevInstance.DevCoreApp.Client.Services.Core.Auth;
using DevInstance.DevCoreApp.Shared.Model.Core;
using DevInstance.DevCoreApp.Shared.Model.Core.Permissions;
using DevInstance.DevCoreApp.Shared.Model.Core.UserAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;
using Xunit;

namespace DevInstance.DevCoreApp.Client.Services.Tests.Core;

public class ApiAuthenticationStateProviderTests
{
    private static CurrentUserItem User() => new()
    {
        Profile = new UserProfileItem { Id = "u1", FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com" },
        Roles = new[] { "Admin" },
        Permissions = new[] { PermissionDefinitions.Admin.Users.View }
    };

    [Fact]
    public void principal_carries_identity_roles_and_permissions()
    {
        var principal = ApiAuthenticationStateProvider.BuildPrincipal(User());

        Assert.True(principal.Identity!.IsAuthenticated);
        Assert.Equal("Ada Lovelace", principal.Identity.Name);
        Assert.Equal("u1", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.True(principal.IsInRole("Admin"));
        Assert.True(principal.HasClaim(PermissionClaims.Type, PermissionDefinitions.Admin.Users.View));
    }

    [Fact]
    public async Task permission_policy_matches_only_granted_keys()
    {
        var provider = new ClientPermissionPolicyProvider(Options.Create(new AuthorizationOptions()));
        var principal = ApiAuthenticationStateProvider.BuildPrincipal(User());

        var granted = await provider.GetPolicyAsync(PermissionDefinitions.Admin.Users.View);
        var denied = await provider.GetPolicyAsync(PermissionDefinitions.Admin.Users.Delete);

        Assert.True(Satisfies(granted!, principal));
        Assert.False(Satisfies(denied!, principal));
    }

    private static bool Satisfies(AuthorizationPolicy policy, ClaimsPrincipal user) =>
        policy.Requirements.All(r => r switch
        {
            ClaimsAuthorizationRequirement c => user.HasClaim(x => x.Type == c.ClaimType && (c.AllowedValues == null || c.AllowedValues.Contains(x.Value))),
            DenyAnonymousAuthorizationRequirement => user.Identity?.IsAuthenticated == true,
            _ => false
        });
}
