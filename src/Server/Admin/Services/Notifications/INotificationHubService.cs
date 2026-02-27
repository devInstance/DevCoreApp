using DevInstance.DevCoreApp.Shared.Model.Notifications;
using System;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Notifications;

public interface INotificationHubService
{
    Task SendNotificationAsync(Guid applicationUserId, NotificationItem notification);
    Task SendUnreadCountAsync(Guid applicationUserId, int unreadCount);
}
