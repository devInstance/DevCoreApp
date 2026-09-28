using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Notifications;

namespace DevInstance.DevCoreApp.Client.Services.Core.Notifications;

/// <summary>
/// The signed-in user's notifications (<c>api/notifications</c>). New ones arrive live through
/// <see cref="INotificationHubClient"/>.
/// </summary>
public interface INotificationService
{
    Task<ServiceActionResult<PagedList<NotificationItem>>> GetListAsync(ListQuery query);

    Task<ServiceActionResult<int>> GetUnreadCountAsync();

    Task<ServiceActionResult<NotificationItem>> MarkAsReadAsync(string id);

    Task<ServiceActionResult<int>> MarkAllReadAsync();
}
