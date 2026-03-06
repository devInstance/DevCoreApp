using System;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using DevInstance.DevCoreApp.Shared.Model.Webhooks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models.Webhooks;

public class WebhookDelivery : DatabaseObject
{
    public Guid SubscriptionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public int? ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public WebhookDeliveryStatus Status { get; set; }

    public WebhookSubscription Subscription { get; set; } = default!;
}
