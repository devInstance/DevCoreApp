using DevInstance.DevCoreApp.Server.Admin.Services.Core.Notifications;
using DevInstance.DevCoreApp.Shared.Model.Core.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Hubs;

public class NotificationHubService : INotificationHubService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationHubService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(Guid applicationUserId, NotificationItem notification)
    {
        await _hubContext.Clients.User(applicationUserId.ToString())
            .SendAsync("ReceiveNotification", notification);
    }

    public async Task SendUnreadCountAsync(Guid applicationUserId, int unreadCount)
    {
        await _hubContext.Clients.User(applicationUserId.ToString())
            .SendAsync("UpdateUnreadCount", unreadCount);
    }
}
