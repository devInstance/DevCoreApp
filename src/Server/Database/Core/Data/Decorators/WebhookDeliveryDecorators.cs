using DevInstance.DevCoreApp.Server.Database.Core.Models.Webhooks;
using DevInstance.DevCoreApp.Shared.Model.Webhooks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;

public static class WebhookDeliveryDecorators
{
    public static WebhookDeliveryItem ToView(this WebhookDelivery delivery)
    {
        return new WebhookDeliveryItem
        {
            Id = delivery.PublicId,
            SubscriptionId = delivery.Subscription?.PublicId ?? string.Empty,
            EventType = delivery.EventType,
            Url = delivery.Subscription?.Url ?? string.Empty,
            Payload = delivery.Payload,
            ResponseStatusCode = delivery.ResponseStatusCode,
            ResponseBody = delivery.ResponseBody,
            AttemptCount = delivery.AttemptCount,
            NextRetryAt = delivery.NextRetryAt,
            Status = delivery.Status,
            CreateDate = delivery.CreateDate,
            UpdateDate = delivery.UpdateDate
        };
    }
}
