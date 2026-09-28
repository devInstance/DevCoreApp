using DevInstance.DevCoreApp.Server.Services.Core.Webhooks;
using DevInstance.DevCoreApp.Shared.Model.Core.Webhooks;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Api.Core.Controllers;

[Route("api/webhooks")]
[Authorize]
public class WebhooksController : ApiControllerBase
{
    private readonly IWebhookAdminService _service;

    public WebhooksController(IWebhookAdminService service) => _service = service;

    [HttpGet]
    [Authorize(Policy = PermissionDefinitions.Admin.Webhooks.View)]
    public Task<ActionResult<PagedList<WebhookSubscriptionItem>>> GetListAsync([FromQuery] ListQuery query)
        => HandleServiceAsync(() => _service.GetSubscriptionsAsync(query.Top, query.Page, query.SortBy, query.Search));

    /// <summary>Delivery attempts across all subscriptions.</summary>
    [HttpGet("deliveries")]
    [Authorize(Policy = PermissionDefinitions.Admin.Webhooks.View)]
    public Task<ActionResult<PagedList<WebhookDeliveryItem>>> GetAllDeliveriesAsync([FromQuery] ListQuery query)
        => HandleServiceAsync(() => _service.GetDeliveriesAsync(query.Top, query.Page, null, query.SortBy));

    [HttpGet("{id}")]
    [Authorize(Policy = PermissionDefinitions.Admin.Webhooks.View)]
    public Task<ActionResult<WebhookSubscriptionItem>> GetAsync(string id)
        => HandleServiceAsync(() => _service.GetSubscriptionAsync(id));

    [HttpGet("{id}/deliveries")]
    [Authorize(Policy = PermissionDefinitions.Admin.Webhooks.View)]
    public Task<ActionResult<PagedList<WebhookDeliveryItem>>> GetDeliveriesAsync(string id, [FromQuery] ListQuery query)
        => HandleServiceAsync(() => _service.GetDeliveriesAsync(query.Top, query.Page, id, query.SortBy));

    [HttpPost]
    [Authorize(Policy = PermissionDefinitions.Admin.Webhooks.Create)]
    public Task<ActionResult<WebhookSubscriptionItem>> CreateAsync([FromBody] WebhookSubscriptionItem item)
        => HandleServiceAsync(() => _service.CreateSubscriptionAsync(item));

    [HttpPut("{id}")]
    [Authorize(Policy = PermissionDefinitions.Admin.Webhooks.Edit)]
    public Task<ActionResult<WebhookSubscriptionItem>> UpdateAsync(string id, [FromBody] WebhookSubscriptionItem item)
        => HandleServiceAsync(() => _service.UpdateSubscriptionAsync(id, item));

    [HttpDelete("{id}")]
    [Authorize(Policy = PermissionDefinitions.Admin.Webhooks.Delete)]
    public Task<ActionResult<bool>> DeleteAsync(string id)
        => HandleServiceAsync(() => _service.DeleteSubscriptionAsync(id));
}
