using DevInstance.DevCoreApp.Shared.Model.Core.Notifications;
using System;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Core.Notifications;

public interface INotificationHubService
{
    Task SendNotificationAsync(Guid applicationUserId, NotificationItem notification);
    Task SendUnreadCountAsync(Guid applicationUserId, int unreadCount);
}
