// Copyright (c) DevInstance LLC. All rights reserved.

using DevInstance.DevCoreApp.Shared.Utils.Core;
using System;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Core.Authentication;

public class CurrentUserContext : ICurrentUserContext
{
    private readonly IAuthorizationContext _authorizationContext;

    public CurrentUserContext(IAuthorizationContext authorizationContext)
    {
        _authorizationContext = authorizationContext;
    }

    public TimeZoneInfo? TimeZone
        => DateTimeExtensions.ResolveTimeZone(_authorizationContext.CurrentProfile?.TimeZoneId);

    public DateTime NowLocal => DateTimeExtensions.NowInZone(TimeZone);

    public string? FullName
    {
        get
        {
            var profile = _authorizationContext.CurrentProfile;
            if (profile == null) return null;

            var name = $"{profile.FirstName} {profile.LastName}".Trim();
            return string.IsNullOrEmpty(name) ? null : name;
        }
    }
}
