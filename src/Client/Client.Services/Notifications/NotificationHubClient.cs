using DevInstance.DevCoreApp.Shared.Model.Notifications;
using DevInstance.LogScope;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace DevInstance.DevCoreApp.Client.Services.Notifications;

public class NotificationHubClient : INotificationHubClient
{
    private HubConnection? _hubConnection;
    private readonly NavigationManager _navigation;
    private readonly IScopeLog _log;

    public event Action<NotificationItem>? OnNotificationReceived;
    public event Action<int>? OnUnreadCountUpdated;
    public event Action<Exception?>? OnConnectionChanged;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public NotificationHubClient(NavigationManager navigation, IScopeManager logManager)
    {
        _navigation = navigation;
        _log = logManager.CreateLogger(this);
    }

    public async Task StartAsync()
    {
        if (_hubConnection != null)
            return;

        using var l = _log.TraceScope();

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_navigation.ToAbsoluteUri("/hubs/notifications"))
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<NotificationItem>("ReceiveNotification", notification =>
        {
            _log.I($"Notification received: {notification.Title}");
            OnNotificationReceived?.Invoke(notification);
        });

        _hubConnection.On<int>("UpdateUnreadCount", count =>
        {
            OnUnreadCountUpdated?.Invoke(count);
        });

        _hubConnection.Closed += error =>
        {
            _log.I("Notification hub connection closed.");
            OnConnectionChanged?.Invoke(error);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += connectionId =>
        {
            _log.I("Notification hub reconnected.");
            OnConnectionChanged?.Invoke(null);
            return Task.CompletedTask;
        };

        try
        {
            await _hubConnection.StartAsync();
            l.I("Notification hub connected.");
            OnConnectionChanged?.Invoke(null);
        }
        catch (Exception ex)
        {
            l.E($"Failed to connect to notification hub: {ex.Message}");
            OnConnectionChanged?.Invoke(ex);
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection == null)
            return;

        using var l = _log.TraceScope();

        await _hubConnection.StopAsync();
        await _hubConnection.DisposeAsync();
        _hubConnection = null;

        l.I("Notification hub disconnected.");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
