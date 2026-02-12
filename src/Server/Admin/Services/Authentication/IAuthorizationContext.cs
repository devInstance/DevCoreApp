using DevInstance.DevCoreApp.Server.Database.Core.Models;
using System.Security.Claims;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Authentication;

public interface IAuthorizationContext
{
    UserProfile CurrentProfile { get; }
    ClaimsPrincipal User { get; }
    void ResetCurrentProfile();

    UserProfile FindUserProfile(ApplicationUser user);
}
