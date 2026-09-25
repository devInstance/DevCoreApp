// Copyright (c) DevInstance LLC. All rights reserved.

using System;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Core.Authentication;

/// <summary>
/// Page-friendly view onto the current user's identity-derived state.
/// Wraps IAuthorizationContext so Razor pages and components can access the
/// resolved user TimeZoneInfo without cracking open the auth context themselves.
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>
    /// Resolved TimeZoneInfo for the current user, or null when the user has no
    /// TimeZoneId configured or the id does not resolve on this OS.
    /// </summary>
    TimeZoneInfo? TimeZone { get; }

    /// <summary>
    /// "Now" expressed in the user's local clock. Use this when seeding
    /// new ViewModels bound to DateTimeLocal pickers in Blazor SSR.
    /// </summary>
    DateTime NowLocal { get; }

    /// <summary>
    /// "FirstName LastName" for the current user, or null when unauthenticated or the
    /// profile carries no name. UserProfile has no DisplayName column, so this is the
    /// one place the concatenation convention lives.
    /// </summary>
    string? FullName { get; }
}
