using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Webhooks;

namespace DevInstance.DevCoreApp.Client.Services.Core.Webhooks;

/// <summary>Webhook subscriptions and deliveries (<c>api/webhooks</c>). Same shape as the server service.</summary>
public interface IWebhookAdminService
{
    Task<ServiceActionResult<PagedList<WebhookSubscriptionItem>>> GetSubscriptionsAsync(int top, int page, string[]? sortBy = null, string? search = null);
    Task<ServiceActionResult<WebhookSubscriptionItem>> GetSubscriptionAsync(string id);
    Task<ServiceActionResult<WebhookSubscriptionItem>> CreateSubscriptionAsync(WebhookSubscriptionItem item);
    Task<ServiceActionResult<WebhookSubscriptionItem>> UpdateSubscriptionAsync(string id, WebhookSubscriptionItem item);
    Task<ServiceActionResult<bool>> DeleteSubscriptionAsync(string id);
    Task<ServiceActionResult<PagedList<WebhookDeliveryItem>>> GetDeliveriesAsync(int top, int page, string? subscriptionId = null, string[]? sortBy = null);
}
