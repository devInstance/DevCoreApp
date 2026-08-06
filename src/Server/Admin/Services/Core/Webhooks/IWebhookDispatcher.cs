using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Core.Webhooks;

public interface IWebhookDispatcher
{
    Task DispatchAsync(string eventType, object eventPayload);
}
