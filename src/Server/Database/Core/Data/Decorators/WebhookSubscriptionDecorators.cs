using DevInstance.DevCoreApp.Server.Database.Core.Models.Webhooks;
using DevInstance.DevCoreApp.Shared.Model.Webhooks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;

public static class WebhookSubscriptionDecorators
{
    public static WebhookSubscriptionItem ToView(this WebhookSubscription subscription)
    {
        return new WebhookSubscriptionItem
        {
            Id = subscription.PublicId,
            EventType = subscription.EventType,
            Url = subscription.Url,
            IsActive = subscription.IsActive,
            CreatedByName = subscription.CreatedBy != null
                ? $"{subscription.CreatedBy.FirstName} {subscription.CreatedBy.LastName}".Trim()
                : null,
            OrganizationName = subscription.Organization?.Name,
            CreateDate = subscription.CreateDate,
            UpdateDate = subscription.UpdateDate
        };
    }

    public static WebhookSubscription ToRecord(this WebhookSubscription subscription, WebhookSubscriptionItem item)
    {
        subscription.EventType = item.EventType;
        subscription.Url = item.Url;
        subscription.IsActive = item.IsActive;
        return subscription;
    }
}
