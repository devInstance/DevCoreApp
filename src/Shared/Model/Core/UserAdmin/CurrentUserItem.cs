using System;

namespace DevInstance.DevCoreApp.Shared.Model.Core.UserAdmin;

/// <summary>
/// Response of <c>GET api/me</c>: everything a client needs about the signed-in user to render
/// its shell. <see cref="Permissions"/> is for hiding UI only — every endpoint enforces its own
/// permission policy on the server.
/// </summary>
public class CurrentUserItem
{
    public UserProfileItem Profile { get; set; }

    public string[] Roles { get; set; } = Array.Empty<string>();

    /// <summary>Effective permission keys (<c>Module.Entity.Action</c>) after role grants and user overrides.</summary>
    public string[] Permissions { get; set; } = Array.Empty<string>();

    /// <summary>"Light", "Dark" or "System".</summary>
    public string Theme { get; set; }

    /// <summary>
    /// Whether this server pushes notifications over SignalR (<c>Notifications:RealTime</c>).
    /// When false the client must not connect to the hub and polls instead.
    /// </summary>
    public bool RealTimeNotifications { get; set; }
}
