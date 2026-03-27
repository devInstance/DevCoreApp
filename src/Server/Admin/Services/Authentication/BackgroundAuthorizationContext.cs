// Copyright (c) DevInstance LLC. All rights reserved.

using System.Security.Claims;
using DevInstance.DevCoreApp.Server.Database.Core.Models;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Authentication;

/// <summary>
/// IAuthorizationContext implementation for background processing (no HttpContext).
/// Populate CurrentProfile before resolving services that depend on IAuthorizationContext.
/// </summary>
public class BackgroundAuthorizationContext : IAuthorizationContext
{
    public UserProfile CurrentProfile { get; set; }

    public ClaimsPrincipal User => new ClaimsPrincipal(new ClaimsIdentity());

    public void ResetCurrentProfile()
    {
        CurrentProfile = null;
    }

    public UserProfile FindUserProfile(ApplicationUser user)
    {
        return null;
    }
}
