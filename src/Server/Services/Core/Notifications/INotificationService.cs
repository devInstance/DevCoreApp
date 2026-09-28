using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Notifications;
using DevInstance.WebServiceToolkit.Common.Model;
using System;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Services.Core.Notifications;

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

    Task<ServiceActionResult<PagedList<NotificationItem>>> GetNotificationsAsync(
        Guid userProfileId, int? page = null, int? pageSize = null);

    // ── Current user (api/notifications) ──

    /// <summary>The signed-in user's notifications, newest first.</summary>
    Task<ServiceActionResult<PagedList<NotificationItem>>> GetMyNotificationsAsync(int? page = null, int? pageSize = null);

    Task<ServiceActionResult<int>> GetMyUnreadCountAsync();

    Task<ServiceActionResult<int>> MarkAllMyReadAsync();
}
