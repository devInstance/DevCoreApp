using DevInstance.DevCoreApp.Shared.Model.Notifications;

namespace DevInstance.DevCoreApp.Client.Services.Notifications;

public interface INotificationHubClient : IAsyncDisposable
{
    event Action<NotificationItem>? OnNotificationReceived;
    event Action<int>? OnUnreadCountUpdated;
    event Action<Exception?>? OnConnectionChanged;

    bool IsConnected { get; }

    Task StartAsync();
    Task StopAsync();
}
