using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Hubs;

[Authorize]
public class NotificationHub : Hub
{
}
