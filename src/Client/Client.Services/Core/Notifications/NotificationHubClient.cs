using DevInstance.DevCoreApp.Shared.Model.Core.Notifications;
using DevInstance.LogScope;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Client.Services.Core.Auth;
using Microsoft.AspNetCore.SignalR.Client;

namespace DevInstance.DevCoreApp.Client.Services.Core.Notifications;

/// <summary>
/// Live notifications from <c>/hubs/notifications</c>. Connects to the API origin (which may differ
/// from the app's) and authenticates with the current access token — SignalR sends it as
/// <c>?access_token=</c>, which the server's JwtBearer setup accepts for <c>/hubs</c>.
/// </summary>
public class NotificationHubClient : INotificationHubClient
{
    private HubConnection? _hubConnection;
    private readonly ApiClientOptions _api;
    private readonly AuthTokenStore _tokens;
    private readonly TokenRefresher _refresher;
    private readonly IScopeLog _log;

    public event Action<NotificationItem>? OnNotificationReceived;
    public event Action<int>? OnUnreadCountUpdated;
    public event Action<Exception?>? OnConnectionChanged;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public NotificationHubClient(ApiClientOptions api, AuthTokenStore tokens, TokenRefresher refresher, IScopeManager logManager)
    {
        _api = api;
        _tokens = tokens;
        _refresher = refresher;
        _log = logManager.CreateLogger(this);
    }

    public async Task StartAsync()
    {
        if (_hubConnection != null)
            return;

        using var l = _log.TraceScope();

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(new Uri(_api.BaseAddress, "hubs/notifications"), options =>
                options.AccessTokenProvider = GetAccessTokenAsync)
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

    // Called on every (re)connect, so a reconnect after expiry gets a fresh token.
    private async Task<string?> GetAccessTokenAsync()
    {
        var tokens = await _tokens.GetAsync();
        if (tokens == null)
            return null;

        return tokens.ExpiresAtUtc <= DateTime.UtcNow.AddSeconds(30)
            ? await _refresher.RefreshAsync(tokens.AccessToken)
            : tokens.AccessToken;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
