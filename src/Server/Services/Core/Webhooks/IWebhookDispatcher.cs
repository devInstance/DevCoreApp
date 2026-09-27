using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Services.Core.Webhooks;

public interface IWebhookDispatcher
{
    Task DispatchAsync(string eventType, object eventPayload);
}
