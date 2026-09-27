using System.Security.Claims;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Services.Core.Appearance;
using DevInstance.DevCoreApp.Server.Services.Core.Authentication;
using DevInstance.DevCoreApp.Server.Services.Core.Notifications;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Shared.Model.Core.UserAdmin;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Exceptions;
using Microsoft.Extensions.Options;

namespace DevInstance.DevCoreApp.Server.Services.Core.UserAdmin;

/// <summary>
/// Composes the current user's profile, roles, effective permissions and theme. Roles and
/// permissions come from the request's <see cref="ClaimsPrincipal"/> (JWT role claims and the
/// Permission claims added by PermissionClaimsTransformation), so this reads no extra tables.
/// Dual-annotated: it has no data of its own to fake, so mock mode uses it as-is.
/// </summary>
[BlazorService]
[BlazorServiceMock]
public class CurrentUserService : ICurrentUserService
{
    private readonly IAuthorizationContext authorizationContext;
    private readonly IPermissionService permissionService;
    private readonly IThemeService themeService;
    private readonly NotificationSettings notificationSettings;
    private readonly IScopeLog log;

    public CurrentUserService(IScopeManager logManager,
                              IAuthorizationContext authorizationContext,
                              IPermissionService permissionService,
                              IThemeService themeService,
                              IOptions<NotificationSettings> notificationSettings)
    {
        log = logManager.CreateLogger(this);
        this.authorizationContext = authorizationContext;
        this.permissionService = permissionService;
        this.themeService = themeService;
        this.notificationSettings = notificationSettings.Value;
    }

    public async Task<ServiceActionResult<CurrentUserItem>> GetAsync()
    {
        using var l = log.TraceScope();

        var profile = authorizationContext.CurrentProfile
            ?? throw new UnauthorizedException("No user profile for the current user.");

        var roles = authorizationContext.User?.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Distinct()
            .ToArray() ?? Array.Empty<string>();

        var permissions = (await permissionService.GetEffectivePermissionsAsync())
            .OrderBy(p => p)
            .ToArray();

        var theme = (await themeService.GetUserThemeAsync()).Result;

        return ServiceActionResult<CurrentUserItem>.OK(new CurrentUserItem
        {
            Profile = profile.ToView(),
            Roles = roles,
            Permissions = permissions,
            Theme = theme,
            RealTimeNotifications = notificationSettings.RealTime
        });
    }
}
