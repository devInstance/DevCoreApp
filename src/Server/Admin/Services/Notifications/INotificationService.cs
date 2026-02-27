using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Common;
using DevInstance.DevCoreApp.Shared.Model.Notifications;
using DevInstance.WebServiceToolkit.Common.Model;
using System;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Notifications;

public interface INotificationService
{
    Task<ServiceActionResult<NotificationItem>> SendAsync(
        Guid userProfileId, NotificationType type, string title, string message,
        string? linkUrl = null, string? groupKey = null, string? category = null);

    Task<ServiceActionResult<int>> SendToRoleAsync(
        string roleName, NotificationType type, string title, string message,
        string? linkUrl = null, string? groupKey = null, string? category = null);

    Task<ServiceActionResult<int>> SendToOrganizationAsync(
        Guid organizationId, NotificationType type, string title, string message,
        string? linkUrl = null, string? groupKey = null, string? category = null);

    Task<ServiceActionResult<NotificationItem>> MarkAsReadAsync(string notificationId);

    Task<ServiceActionResult<int>> MarkAllReadAsync(Guid userProfileId);

    Task<ServiceActionResult<int>> GetUnreadCountAsync(Guid userProfileId);

    Task<ServiceActionResult<ModelList<NotificationItem>>> GetNotificationsAsync(
        Guid userProfileId, int? page = null, int? pageSize = null);
}
