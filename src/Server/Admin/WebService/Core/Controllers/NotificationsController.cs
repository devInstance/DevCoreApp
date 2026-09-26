using DevInstance.DevCoreApp.Server.Admin.Services.Core.Notifications;
using DevInstance.DevCoreApp.Shared.Model.Core.Notifications;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Core.Controllers;

/// <summary>
/// The signed-in user's notifications. New ones are pushed live over SignalR
/// (<c>/hubs/notifications</c>); these endpoints load the list and change read state.
/// </summary>
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service) => _service = service;

    [HttpGet]
    public Task<ActionResult<PagedList<NotificationItem>>> GetListAsync([FromQuery] ListQuery query)
        => HandleServiceAsync(() => _service.GetMyNotificationsAsync(query.Page, query.Top));

    [HttpGet("unread-count")]
    public Task<ActionResult<int>> GetUnreadCountAsync()
        => HandleServiceAsync(() => _service.GetMyUnreadCountAsync());

    [HttpPost("{id}/read")]
    public Task<ActionResult<NotificationItem>> MarkAsReadAsync(string id)
        => HandleServiceAsync(() => _service.MarkAsReadAsync(id));

    [HttpPost("read-all")]
    public Task<ActionResult<int>> MarkAllReadAsync()
        => HandleServiceAsync(() => _service.MarkAllMyReadAsync());
}
