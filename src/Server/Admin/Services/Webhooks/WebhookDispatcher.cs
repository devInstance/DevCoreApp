using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Background;
using DevInstance.DevCoreApp.Server.Admin.Services.Background.Requests;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Shared.Model.Webhooks;
using DevInstance.LogScope;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Webhooks;

[BlazorService]
public class WebhookDispatcher : IWebhookDispatcher
{
    private readonly IQueryRepository _repository;
    private readonly IBackgroundWorker _backgroundWorker;
    private readonly IScopeLog _log;

    public WebhookDispatcher(
        IScopeManager logManager,
        IQueryRepository repository,
        IBackgroundWorker backgroundWorker)
    {
        _log = logManager.CreateLogger(this);
        _repository = repository;
        _backgroundWorker = backgroundWorker;
    }

    public async Task DispatchAsync(string eventType, object eventPayload)
    {
        using var l = _log.TraceScope();

        var subscriptions = await _repository.GetWebhookSubscriptionQuery(null!)
            .ByEventType(eventType)
            .ActiveOnly()
            .Select()
            .ToListAsync();

        if (subscriptions.Count == 0)
        {
            l.I($"No active webhook subscriptions for event '{eventType}'.");
            return;
        }

        var payloadJson = JsonSerializer.Serialize(eventPayload, eventPayload.GetType());

        foreach (var subscription in subscriptions)
        {
            // Create a WebhookDelivery record
            var deliveryQuery = _repository.GetWebhookDeliveryQuery(null!);
            var delivery = deliveryQuery.CreateNew();
            delivery.SubscriptionId = subscription.Id;
            delivery.EventType = eventType;
            delivery.Payload = payloadJson;
            delivery.Status = WebhookDeliveryStatus.Pending;
            delivery.AttemptCount = 0;

            await deliveryQuery.AddAsync(delivery);

            // Submit a background job for each delivery
            await _backgroundWorker.SubmitAsync(new BackgroundRequestItem
            {
                RequestType = BackgroundRequestType.DeliverWebhook,
                Content = new WebhookDeliveryRequest
                {
                    DeliveryId = delivery.PublicId,
                    SubscriptionPublicId = subscription.PublicId,
                    EventType = eventType,
                    Payload = payloadJson
                }
            });

            l.I($"Webhook delivery queued: {subscription.Url} for event '{eventType}' (delivery: {delivery.PublicId})");
        }
    }
}
