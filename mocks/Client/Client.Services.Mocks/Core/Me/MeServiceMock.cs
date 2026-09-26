using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Me;
using DevInstance.DevCoreApp.Shared.Model.Core;
using DevInstance.DevCoreApp.Shared.Model.Core.Permissions;
using DevInstance.DevCoreApp.Shared.Model.Core.UserAdmin;
using DevInstance.WebServiceToolkit.Common.Tools;

namespace DevInstance.DevCoreApp.Client.Services.Mocks.Core.Me;

/// <summary>An Owner holding every permission, so every page is reachable in mock mode.</summary>
[BlazorServiceMock]
public class MeServiceMock : IMeService
{
    private readonly CurrentUserItem user = new()
    {
        Profile = new UserProfileItem
        {
            Id = IdGenerator.New(),
            FirstName = "Mock",
            LastName = "Owner",
            Email = "owner@example.com",
            Roles = ApplicationRoles.Owner,
            Status = "LIVE",
            CreateDate = DateTime.UtcNow.AddYears(-1),
            UpdateDate = DateTime.UtcNow
        },
        Roles = new[] { ApplicationRoles.Owner },
        Permissions = PermissionDefinitions.GetAll().ToArray(),
        Theme = "System",
        // No server, so no hub: the notification badge polls the mock instead.
        RealTimeNotifications = false
    };

    public async Task<ServiceActionResult<CurrentUserItem>> GetAsync()
    {
        await Task.Delay(100);
        return ServiceActionResult<CurrentUserItem>.OK(user);
    }

    public async Task<ServiceActionResult<UserProfileItem>> UpdateProfileAsync(UserProfileItem profile)
    {
        await Task.Delay(300);
        var current = user.Profile;
        current.FirstName = profile.FirstName;
        current.MiddleName = profile.MiddleName;
        current.LastName = profile.LastName;
        current.PhoneNumber = profile.PhoneNumber;
        current.TimeZoneId = profile.TimeZoneId;
        current.UpdateDate = DateTime.UtcNow;
        return ServiceActionResult<UserProfileItem>.OK(current);
    }

    public Task<ServiceActionResult<string>> SetThemeAsync(string theme)
    {
        user.Theme = theme;
        return Task.FromResult(ServiceActionResult<string>.OK(theme));
    }
}
