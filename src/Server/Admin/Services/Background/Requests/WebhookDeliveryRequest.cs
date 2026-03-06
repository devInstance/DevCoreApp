namespace DevInstance.DevCoreApp.Server.Admin.Services.Background.Requests;

public class WebhookDeliveryRequest
{
    public string DeliveryId { get; set; } = string.Empty;
    public string SubscriptionPublicId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
}
