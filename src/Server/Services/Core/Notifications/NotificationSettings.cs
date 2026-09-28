namespace DevInstance.DevCoreApp.Server.Services.Core.Notifications;

/// <summary>
/// <c>Notifications</c> section of appsettings.
/// </summary>
public class NotificationSettings
{
    public const string SectionName = "Notifications";

    /// <summary>
    /// Push notifications live over SignalR (<c>/hubs/notifications</c>). When false the hub is
    /// not mapped and clients poll <c>api/notifications/unread-count</c> instead — for hosts that
    /// cannot keep connections open (serverless, some proxies), or several instances without a
    /// SignalR backplane (Redis / Azure SignalR Service), where a push from one instance never
    /// reaches clients connected to another.
    /// </summary>
    public bool RealTime { get; set; } = true;
}
