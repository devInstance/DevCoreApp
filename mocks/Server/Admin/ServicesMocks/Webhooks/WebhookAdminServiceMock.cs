using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Webhooks;
using DevInstance.DevCoreApp.Shared.Model.Webhooks;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Mocks.Webhooks;

[BlazorServiceMock]
public class WebhookAdminServiceMock : IWebhookAdminService
{
    private readonly List<WebhookSubscriptionItem> _subscriptions;
    private readonly List<WebhookDeliveryItem> _deliveries;
    private readonly int delay = 500;

    public WebhookAdminServiceMock()
    {
        _subscriptions = GenerateSubscriptions();
        _deliveries = GenerateDeliveries();
    }

    private static List<WebhookSubscriptionItem> GenerateSubscriptions()
    {
        var now = DateTime.UtcNow;
        return new List<WebhookSubscriptionItem>
        {
            new()
            {
                Id = IdGenerator.New(), EventType = WebhookEventTypes.UserCreated,
                Url = "https://api.example.com/webhooks/users", IsActive = true,
                OrganizationId = "org-acme",
                CreatedByName = "John Doe", OrganizationName = "Acme Corp",
                Secret = "mock-secret-user-created",
                CreateDate = now.AddDays(-30), UpdateDate = now.AddDays(-5)
            },
            new()
            {
                Id = IdGenerator.New(), EventType = WebhookEventTypes.UserUpdated,
                Url = "https://api.example.com/webhooks/users", IsActive = true,
                OrganizationId = "org-acme",
                CreatedByName = "John Doe", OrganizationName = "Acme Corp",
                Secret = "mock-secret-user-updated",
                CreateDate = now.AddDays(-30), UpdateDate = now.AddDays(-5)
            },
            new()
            {
                Id = IdGenerator.New(), EventType = WebhookEventTypes.OrganizationCreated,
                Url = "https://hooks.slack.com/services/T00/B00/xxx", IsActive = true,
                OrganizationId = "org-east",
                CreatedByName = "Jane Smith", OrganizationName = "East Region",
                Secret = "mock-secret-org-created",
                CreateDate = now.AddDays(-14), UpdateDate = now.AddDays(-14)
            },
            new()
            {
                Id = IdGenerator.New(), EventType = WebhookEventTypes.UserDeleted,
                Url = "https://old-system.internal/notify", IsActive = false,
                OrganizationId = "org-acme",
                CreatedByName = "Admin User", OrganizationName = "Acme Corp",
                Secret = "mock-secret-user-deleted",
                CreateDate = now.AddDays(-60), UpdateDate = now.AddDays(-10)
            },
        };
    }

    private List<WebhookDeliveryItem> GenerateDeliveries()
    {
        var now = DateTime.UtcNow;
        var sub1 = _subscriptions[0].Id;
        var sub2 = _subscriptions[2].Id;

        return new List<WebhookDeliveryItem>
        {
            new()
            {
                Id = IdGenerator.New(), SubscriptionId = sub1, EventType = WebhookEventTypes.UserCreated,
                Url = "https://api.example.com/webhooks/users",
                Payload = "{\"userId\":\"abc-123\",\"email\":\"new@example.com\"}",
                ResponseStatusCode = 200, ResponseBody = "OK", AttemptCount = 1,
                Status = WebhookDeliveryStatus.Delivered,
                CreateDate = now.AddHours(-2), UpdateDate = now.AddHours(-2)
            },
            new()
            {
                Id = IdGenerator.New(), SubscriptionId = sub1, EventType = WebhookEventTypes.UserCreated,
                Url = "https://api.example.com/webhooks/users",
                Payload = "{\"userId\":\"def-456\",\"email\":\"another@example.com\"}",
                ResponseStatusCode = 500, ResponseBody = "Internal Server Error", AttemptCount = 3,
                Status = WebhookDeliveryStatus.Failed,
                CreateDate = now.AddHours(-5), UpdateDate = now.AddHours(-4)
            },
            new()
            {
                Id = IdGenerator.New(), SubscriptionId = sub2, EventType = WebhookEventTypes.OrganizationCreated,
                Url = "https://hooks.slack.com/services/T00/B00/xxx",
                Payload = "{\"orgId\":\"org-789\",\"name\":\"New Office\"}",
                ResponseStatusCode = null, AttemptCount = 1,
                NextRetryAt = now.AddMinutes(5), Status = WebhookDeliveryStatus.Pending,
                CreateDate = now.AddMinutes(-10), UpdateDate = now.AddMinutes(-10)
            },
            new()
            {
                Id = IdGenerator.New(), SubscriptionId = sub1, EventType = WebhookEventTypes.UserCreated,
                Url = "https://api.example.com/webhooks/users",
                Payload = "{\"userId\":\"ghi-012\",\"email\":\"third@example.com\"}",
                ResponseStatusCode = 200, ResponseBody = "OK", AttemptCount = 1,
                Status = WebhookDeliveryStatus.Delivered,
                CreateDate = now.AddDays(-1), UpdateDate = now.AddDays(-1)
            },
        };
    }

    public async Task<ServiceActionResult<ModelList<WebhookSubscriptionItem>>> GetSubscriptionsAsync(
        int top, int page, string[]? sortBy = null, string? search = null)
    {
        await Task.Delay(delay);

        IEnumerable<WebhookSubscriptionItem> query = _subscriptions;

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s =>
                s.EventType.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.Url.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = query.ToList();
        var items = filtered.Skip(page * top).Take(top).ToArray();

        return ServiceActionResult<ModelList<WebhookSubscriptionItem>>.OK(
            ModelListResult.CreateList(items, filtered.Count, top, page, sortBy, search));
    }

    public async Task<ServiceActionResult<WebhookSubscriptionItem>> GetSubscriptionAsync(string id)
    {
        await Task.Delay(delay);

        var item = _subscriptions.Find(s => s.Id == id);
        if (item == null)
            throw new InvalidOperationException("Webhook subscription not found.");

        return ServiceActionResult<WebhookSubscriptionItem>.OK(item);
    }

    public async Task<ServiceActionResult<WebhookSubscriptionItem>> CreateSubscriptionAsync(WebhookSubscriptionItem item)
    {
        await Task.Delay(delay);

        item.Id = IdGenerator.New();
        item.CreateDate = DateTime.UtcNow;
        item.UpdateDate = DateTime.UtcNow;
        item.CreatedByName = "Current User";
        item.OrganizationName = "Acme Corp";
        item.Secret = "mock-generated-secret";
        _subscriptions.Insert(0, item);

        return ServiceActionResult<WebhookSubscriptionItem>.OK(item);
    }

    public async Task<ServiceActionResult<WebhookSubscriptionItem>> UpdateSubscriptionAsync(string id, WebhookSubscriptionItem item)
    {
        await Task.Delay(delay);

        var index = _subscriptions.FindIndex(s => s.Id == id);
        if (index < 0)
            throw new InvalidOperationException("Webhook subscription not found.");

        var existing = _subscriptions[index];
        existing.EventType = item.EventType;
        existing.Url = item.Url;
        existing.IsActive = item.IsActive;
        existing.OrganizationId = item.OrganizationId;
        existing.UpdateDate = DateTime.UtcNow;

        return ServiceActionResult<WebhookSubscriptionItem>.OK(existing);
    }

    public async Task<ServiceActionResult<bool>> DeleteSubscriptionAsync(string id)
    {
        await Task.Delay(delay);

        var item = _subscriptions.Find(s => s.Id == id);
        if (item == null)
            throw new InvalidOperationException("Webhook subscription not found.");

        _subscriptions.Remove(item);

        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<ModelList<WebhookDeliveryItem>>> GetDeliveriesAsync(
        int top, int page, string? subscriptionId = null, string[]? sortBy = null)
    {
        await Task.Delay(delay);

        IEnumerable<WebhookDeliveryItem> query = _deliveries;

        if (!string.IsNullOrEmpty(subscriptionId))
            query = query.Where(d => d.SubscriptionId == subscriptionId);

        var filtered = query.OrderByDescending(d => d.CreateDate).ToList();
        var items = filtered.Skip(page * top).Take(top).ToArray();

        return ServiceActionResult<ModelList<WebhookDeliveryItem>>.OK(
            ModelListResult.CreateList(items, filtered.Count, top, page, sortBy, null));
    }
}
