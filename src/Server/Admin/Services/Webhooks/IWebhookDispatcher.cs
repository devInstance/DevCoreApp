using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Webhooks;

public interface IWebhookDispatcher
{
    Task DispatchAsync(string eventType, object eventPayload);
}
