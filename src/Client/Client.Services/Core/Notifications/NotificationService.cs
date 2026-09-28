using DevInstance.BlazorToolkit.Http;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Notifications;
using DevInstance.LogScope;

namespace DevInstance.DevCoreApp.Client.Services.Core.Notifications;

[BlazorService]
public class NotificationService : ApiServiceBase, INotificationService
{
    public NotificationService(IHttpApiContextFactory apiFactory, IScopeManager logManager) : base(apiFactory, logManager) { }

    public Task<ServiceActionResult<PagedList<NotificationItem>>> GetListAsync(ListQuery query) =>
        CallAsync(() => Api<NotificationItem>("api/notifications").Get().Query(query).ExecuteAsync<PagedList<NotificationItem>>());

    public Task<ServiceActionResult<int>> GetUnreadCountAsync() =>
        CallAsync(() => Api<int>("api/notifications/unread-count").Get().ExecuteAsync());

    public Task<ServiceActionResult<NotificationItem>> MarkAsReadAsync(string id) =>
        CallAsync(() => Api<NotificationItem>($"api/notifications/{Uri.EscapeDataString(id)}/read").Post<object?>(null).ExecuteAsync());

    public Task<ServiceActionResult<int>> MarkAllReadAsync() =>
        CallAsync(() => Api<int>("api/notifications/read-all").Post<object?>(null).ExecuteAsync());
}
