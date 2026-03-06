using System;
using System.Linq;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Webhooks;
using DevInstance.DevCoreApp.Shared.Model.Webhooks;
using DevInstance.WebServiceToolkit.Database.Queries;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IWebhookDeliveryQuery : IModelQuery<WebhookDelivery, IWebhookDeliveryQuery>,
        IQPageable<IWebhookDeliveryQuery>,
        IQSortable<IWebhookDeliveryQuery>
{
    IQueryable<WebhookDelivery> Select();

    IWebhookDeliveryQuery ByPublicId(string id);
    IWebhookDeliveryQuery BySubscriptionId(Guid subscriptionId);
    IWebhookDeliveryQuery ByStatus(WebhookDeliveryStatus status);
    IWebhookDeliveryQuery ByEventType(string eventType);
}
