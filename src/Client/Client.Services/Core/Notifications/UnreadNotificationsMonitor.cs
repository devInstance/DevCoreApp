using DevInstance.LogScope;

namespace DevInstance.DevCoreApp.Client.Services.Core.Notifications;

/// <summary>
/// Keeps the signed-in user's unread-notification count current, whatever the hosting allows:
/// live over SignalR when the server offers it and the connection holds, otherwise by polling
/// <c>api/notifications/unread-count</c>. UI components only read <see cref="UnreadCount"/> and
/// listen to <see cref="Changed"/>.
/// </summary>
public interface IUnreadNotificationsMonitor : IAsyncDisposable
{
    int UnreadCount { get; }

    /// <summary>True while updates arrive live over SignalR; false while polling.</summary>
    bool IsLive { get; }

    event Action? Changed;

    /// <summary>
    /// Loads the count and starts keeping it current. <paramref name="realTime"/> is the server's
    /// <c>RealTimeNotifications</c> flag from <c>api/me</c>; false means never try the hub.
    /// Calling it again while running does nothing.
    /// </summary>
    Task StartAsync(bool realTime);

    /// <summary>Re-reads the count now (e.g. when the window regains focus). Skipped while live.</summary>
    Task RefreshAsync(bool force = false);

    /// <summary>Stops polling and disconnects the hub (sign-out).</summary>
    Task StopAsync();
}

public sealed class UnreadNotificationsMonitor : IUnreadNotificationsMonitor
{
    private readonly INotificationService notifications;
    private readonly INotificationHubClient hub;
    private readonly IScopeLog log;
    private CancellationTokenSource? polling;

    public UnreadNotificationsMonitor(INotificationService notifications, INotificationHubClient hub, IScopeManager logManager)
    {
        this.notifications = notifications;
        this.hub = hub;
        log = logManager.CreateLogger(this);
    }

    /// <summary>How often to re-read the count while not live.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(60);

    public int UnreadCount { get; private set; }

    public bool IsLive => hub.IsConnected;

    public event Action? Changed;

    public async Task StartAsync(bool realTime)
    {
        if (polling != null)
        {
            return;
        }

        using var l = log.TraceScope();
        polling = new CancellationTokenSource();

        await RefreshAsync(force: true);

        if (realTime)
        {
            hub.OnUnreadCountUpdated += OnPushed;
            hub.OnConnectionChanged += OnConnectionChanged;
            try
            {
                await hub.StartAsync();
            }
            catch (Exception ex)
            {
                // WebSockets, SSE and long polling all failed (host cannot hold connections).
                l.W($"Notification hub unavailable, polling instead: {ex.Message}");
            }
        }

        _ = PollAsync(polling.Token);
    }

    public async Task RefreshAsync(bool force = false)
    {
        if (!force && IsLive)
        {
            return;
        }

        var result = await notifications.GetUnreadCountAsync();
        if (result.Success)
        {
            Set(result.Result);
        }
    }

    public async Task StopAsync()
    {
        if (polling == null)
        {
            return;
        }

        polling.Cancel();
        polling.Dispose();
        polling = null;

        hub.OnUnreadCountUpdated -= OnPushed;
        hub.OnConnectionChanged -= OnConnectionChanged;
        await hub.StopAsync();
        Set(0);
    }

    private async Task PollAsync(CancellationToken cancellation)
    {
        try
        {
            using var timer = new PeriodicTimer(PollInterval);
            while (await timer.WaitForNextTickAsync(cancellation))
            {
                await RefreshAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Stopped.
        }
    }

    private void OnPushed(int count) => Set(count);

    // A push sent while the connection was down is lost; re-sync once it is back.
    private void OnConnectionChanged(Exception? error)
    {
        if (error == null)
        {
            _ = RefreshAsync(force: true);
        }
        Changed?.Invoke();
    }

    private void Set(int count)
    {
        if (count != UnreadCount)
        {
            UnreadCount = count;
            Changed?.Invoke();
        }
    }

    public ValueTask DisposeAsync() => new(StopAsync());
}
