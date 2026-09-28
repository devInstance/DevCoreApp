using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Client.Services.Core.Notifications;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Notifications;
using DevInstance.DevCoreApp.Shared.TestUtils.Core;
using Xunit;

namespace DevInstance.DevCoreApp.Client.Services.Tests.Core;

public class UnreadNotificationsMonitorTests
{
    private sealed class FakeNotifications : INotificationService
    {
        public int Count;
        public int Calls;

        public Task<ServiceActionResult<int>> GetUnreadCountAsync()
        {
            Interlocked.Increment(ref Calls);
            return Task.FromResult(ServiceActionResult<int>.OK(Count));
        }

        public Task<ServiceActionResult<PagedList<NotificationItem>>> GetListAsync(ListQuery query) => throw new NotSupportedException();
        public Task<ServiceActionResult<NotificationItem>> MarkAsReadAsync(string id) => throw new NotSupportedException();
        public Task<ServiceActionResult<int>> MarkAllReadAsync() => throw new NotSupportedException();
    }

    private sealed class FakeHub : INotificationHubClient
    {
        public bool FailToStart;
        public int Starts;
        public bool IsConnected { get; set; }

        public event Action<NotificationItem>? OnNotificationReceived;
        public event Action<int>? OnUnreadCountUpdated;
        public event Action<Exception?>? OnConnectionChanged;

        public Task StartAsync()
        {
            Starts++;
            if (FailToStart) throw new HttpRequestException("no transport");
            IsConnected = true;
            // Like NotificationHubClient: the first connect is reported as a connection change.
            OnConnectionChanged?.Invoke(null);
            return Task.CompletedTask;
        }

        public Task StopAsync() { IsConnected = false; return Task.CompletedTask; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void Push(int count) => OnUnreadCountUpdated?.Invoke(count);
        public void Reconnected() { IsConnected = true; OnConnectionChanged?.Invoke(null); }
        public void Received(NotificationItem n) => OnNotificationReceived?.Invoke(n);
    }

    private static (UnreadNotificationsMonitor Monitor, FakeNotifications Api, FakeHub Hub) Create()
    {
        var api = new FakeNotifications { Count = 3 };
        var hub = new FakeHub();
        var monitor = new UnreadNotificationsMonitor(api, hub, new IScopeManagerMock())
        {
            PollInterval = TimeSpan.FromMilliseconds(40)
        };
        return (monitor, api, hub);
    }

    private static async Task Eventually(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
            await Task.Delay(10);
        Assert.True(condition());
    }

    [Fact]
    public async Task real_time_off_never_touches_the_hub_and_polls()
    {
        var (monitor, api, hub) = Create();

        await monitor.StartAsync(realTime: false);
        Assert.Equal(3, monitor.UnreadCount);

        api.Count = 5;
        await Eventually(() => monitor.UnreadCount == 5);

        Assert.Equal(0, hub.Starts);
        Assert.False(monitor.IsLive);
        await monitor.StopAsync();
    }

    [Fact]
    public async Task live_hub_pushes_and_polling_pauses()
    {
        var (monitor, api, hub) = Create();

        await monitor.StartAsync(realTime: true);
        Assert.True(monitor.IsLive);

        var callsAfterStart = api.Calls;
        await Task.Delay(150);
        Assert.Equal(callsAfterStart, api.Calls);

        hub.Push(7);
        Assert.Equal(7, monitor.UnreadCount);
        await monitor.StopAsync();
    }

    [Fact]
    public async Task hub_that_cannot_connect_falls_back_to_polling()
    {
        var (monitor, api, hub) = Create();
        hub.FailToStart = true;

        await monitor.StartAsync(realTime: true);
        api.Count = 9;

        await Eventually(() => monitor.UnreadCount == 9);
        Assert.False(monitor.IsLive);
        await monitor.StopAsync();
    }

    [Fact]
    public async Task dropped_connection_polls_and_reconnect_resyncs()
    {
        var (monitor, api, hub) = Create();
        await monitor.StartAsync(realTime: true);

        hub.IsConnected = false;          // connection lost: polling resumes
        api.Count = 4;
        await Eventually(() => monitor.UnreadCount == 4);

        api.Count = 6;                    // missed while reconnecting
        hub.Reconnected();
        await Eventually(() => monitor.UnreadCount == 6);
        await monitor.StopAsync();
    }

    [Fact]
    public async Task focus_refresh_is_skipped_while_live()
    {
        var (monitor, api, _) = Create();
        await monitor.StartAsync(realTime: true);
        var calls = api.Calls;

        await monitor.RefreshAsync();

        Assert.Equal(calls, api.Calls);
        await monitor.StopAsync();
    }

    [Fact]
    public async Task stop_disconnects_resets_and_stops_polling()
    {
        var (monitor, api, hub) = Create();
        await monitor.StartAsync(realTime: false);

        await monitor.StopAsync();
        var calls = api.Calls;
        await Task.Delay(150);

        Assert.Equal(0, monitor.UnreadCount);
        Assert.Equal(calls, api.Calls);
        Assert.False(hub.IsConnected);
    }

    [Fact]
    public async Task initial_connect_does_not_fetch_the_count_twice()
    {
        var (monitor, api, _) = Create();

        await monitor.StartAsync(realTime: true);
        await Task.Delay(50);

        Assert.Equal(1, api.Calls);
        await monitor.StopAsync();
    }

    [Fact]
    public async Task start_twice_is_a_no_op()
    {
        var (monitor, _, hub) = Create();

        await monitor.StartAsync(realTime: true);
        await monitor.StartAsync(realTime: true);

        Assert.Equal(1, hub.Starts);
        await monitor.StopAsync();
    }
}
