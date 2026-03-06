using System;
using System.Linq;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Webhooks;
using DevInstance.WebServiceToolkit.Database.Queries;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IWebhookSubscriptionQuery : IModelQuery<WebhookSubscription, IWebhookSubscriptionQuery>,
        IQSearchable<IWebhookSubscriptionQuery>,
        IQPageable<IWebhookSubscriptionQuery>,
        IQSortable<IWebhookSubscriptionQuery>
{
    IQueryable<WebhookSubscription> Select();

    IWebhookSubscriptionQuery ByEventType(string eventType);
    IWebhookSubscriptionQuery ActiveOnly();
}
