using DevInstance.BlazorToolkit.Http;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Client.Services.Core.Time;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.LogScope;
using DevInstance.DevCoreApp.Shared.Model.Core.Webhooks;

namespace DevInstance.DevCoreApp.Client.Services.Core.Webhooks;

[BlazorService]
public class WebhookAdminService : ApiServiceBase, IWebhookAdminService
{
    public WebhookAdminService(IHttpApiContextFactory apiFactory, IScopeManager logManager) : base(apiFactory, logManager) { }

    public Task<ServiceActionResult<PagedList<WebhookSubscriptionItem>>> GetSubscriptionsAsync(int top, int page, string[]? sortBy = null, string? search = null) =>
        CallAsync(() => Api<WebhookSubscriptionItem>("api/webhooks").Get()
            .Query(new ListQuery { Top = top, Page = page, SortBy = sortBy!, Search = search! })
            .ExecuteAsync<PagedList<WebhookSubscriptionItem>>());

    public Task<ServiceActionResult<WebhookSubscriptionItem>> GetSubscriptionAsync(string id) =>
        CallAsync(() => Api<WebhookSubscriptionItem>($"api/webhooks/{Segment(id)}").Get().ExecuteAsync());

    public Task<ServiceActionResult<WebhookSubscriptionItem>> CreateSubscriptionAsync(WebhookSubscriptionItem item) =>
        CallAsync(() => Api<WebhookSubscriptionItem>("api/webhooks").Post(item).ExecuteAsync());

    public Task<ServiceActionResult<WebhookSubscriptionItem>> UpdateSubscriptionAsync(string id, WebhookSubscriptionItem item) =>
        CallAsync(() => Api<WebhookSubscriptionItem>($"api/webhooks/{Segment(id)}").Put(item).ExecuteAsync());

    public Task<ServiceActionResult<bool>> DeleteSubscriptionAsync(string id) =>
        CallAsync(() => Api<bool>($"api/webhooks/{Segment(id)}").Delete().ExecuteAsync());

    public Task<ServiceActionResult<PagedList<WebhookDeliveryItem>>> GetDeliveriesAsync(int top, int page, string? subscriptionId = null, string[]? sortBy = null) =>
        CallAsync(() => Api<WebhookDeliveryItem>(string.IsNullOrEmpty(subscriptionId)
                ? "api/webhooks/deliveries"
                : $"api/webhooks/{Segment(subscriptionId)}/deliveries")
            .Get()
            .Query(new ListQuery { Top = top, Page = page, SortBy = sortBy! })
            .ExecuteAsync<PagedList<WebhookDeliveryItem>>());
}
