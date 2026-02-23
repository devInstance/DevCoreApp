using DevInstance.BlazorToolkit.Tools;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Authentication;

[BlazorService]
public class PermissionService : IPermissionService
{
    private readonly IAuthorizationContext _authContext;

    public PermissionService(IAuthorizationContext authContext)
    {
        _authContext = authContext;
    }

    public Task<bool> HasPermissionAsync(string permissionKey)
    {
        var result = _authContext.User?.HasClaim(
            PermissionClaimsTransformation.PermissionClaimType, permissionKey) ?? false;

        return Task.FromResult(result);
    }

    public Task<bool> HasAnyPermissionAsync(params string[] permissionKeys)
    {
        var user = _authContext.User;
        if (user == null)
            return Task.FromResult(false);

        var result = permissionKeys.Any(key =>
            user.HasClaim(PermissionClaimsTransformation.PermissionClaimType, key));

        return Task.FromResult(result);
    }

    public Task<IReadOnlySet<string>> GetEffectivePermissionsAsync()
    {
        var permissions = _authContext.User?.Claims
            .Where(c => c.Type == PermissionClaimsTransformation.PermissionClaimType)
            .Select(c => c.Value)
            .ToHashSet()
            ?? new HashSet<string>();

        return Task.FromResult<IReadOnlySet<string>>(permissions);
    }
}
