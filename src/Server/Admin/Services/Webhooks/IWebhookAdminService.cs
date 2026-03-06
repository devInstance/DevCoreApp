using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Webhooks;
using DevInstance.WebServiceToolkit.Common.Model;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Webhooks;

public interface IWebhookAdminService
{
    Task<ServiceActionResult<ModelList<WebhookSubscriptionItem>>> GetSubscriptionsAsync(
        int top, int page, string[]? sortBy = null, string? search = null);

    Task<ServiceActionResult<WebhookSubscriptionItem>> GetSubscriptionAsync(string id);

    Task<ServiceActionResult<WebhookSubscriptionItem>> CreateSubscriptionAsync(WebhookSubscriptionItem item);

    Task<ServiceActionResult<WebhookSubscriptionItem>> UpdateSubscriptionAsync(string id, WebhookSubscriptionItem item);

    Task<ServiceActionResult<bool>> DeleteSubscriptionAsync(string id);

    Task<ServiceActionResult<ModelList<WebhookDeliveryItem>>> GetDeliveriesAsync(
        int top, int page, string? subscriptionId = null, string[]? sortBy = null);
}
