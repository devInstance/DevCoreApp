using System;
using DevInstance.WebServiceToolkit.Common.Model;

namespace DevInstance.DevCoreApp.Shared.Model.Webhooks;

public class WebhookDeliveryItem : ModelItem
{
    public string SubscriptionId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public int? ResponseStatusCode { get; set; }

    public string? ResponseBody { get; set; }

    public int AttemptCount { get; set; }

    public DateTime? NextRetryAt { get; set; }

    public WebhookDeliveryStatus Status { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime UpdateDate { get; set; }
}
