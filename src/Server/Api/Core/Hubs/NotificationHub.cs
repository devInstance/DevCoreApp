using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DevInstance.DevCoreApp.Server.Api.Core.Hubs;

[Authorize]
public class NotificationHub : Hub
{
}
