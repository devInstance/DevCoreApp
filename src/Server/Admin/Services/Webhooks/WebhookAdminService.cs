using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Shared.Model.Webhooks;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;
using DevInstance.WebServiceToolkit.Database.Queries.Extensions;
using DevInstance.WebServiceToolkit.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Webhooks;

[BlazorService]
public class WebhookAdminService : BaseService, IWebhookAdminService
{
    private IScopeLog log;

    public WebhookAdminService(IScopeManager logManager,
                                ITimeProvider timeProvider,
                                IQueryRepository query,
                                IAuthorizationContext authorizationContext)
        : base(logManager, timeProvider, query, authorizationContext)
    {
        log = logManager.CreateLogger(this);
    }

    public async Task<ServiceActionResult<ModelList<WebhookSubscriptionItem>>> GetSubscriptionsAsync(
        int top, int page, string[]? sortBy = null, string? search = null)
    {
        using var l = log.TraceScope();

        var query = Repository.GetWebhookSubscriptionQuery(AuthorizationContext.CurrentProfile);

        if (!string.IsNullOrEmpty(search))
            query = query.Search(search);

        var sortField = sortBy?.FirstOrDefault()?.TrimStart('-');
        var isAsc = sortBy?.FirstOrDefault()?.StartsWith("-") != true;

        query = !string.IsNullOrEmpty(sortField)
            ? query.SortBy(sortField, isAsc)
            : query.SortBy("createdate", false);

        var totalCount = await query.Clone().Select().CountAsync();
        var subscriptions = await query.Paginate(top, page).Select()
            .Include(ws => ws.CreatedBy)
            .Include(ws => ws.Organization)
            .ToListAsync();

        var items = subscriptions.Select(ws => ws.ToView()).ToArray();

        var modelList = ModelListResult.CreateList(items, totalCount, top, page, sortBy, search);
        return ServiceActionResult<ModelList<WebhookSubscriptionItem>>.OK(modelList);
    }

    public async Task<ServiceActionResult<WebhookSubscriptionItem>> GetSubscriptionAsync(string id)
    {
        using var l = log.TraceScope();

        var subscription = await Repository.GetWebhookSubscriptionQuery(AuthorizationContext.CurrentProfile)
            .ByPublicId(id).Select()
            .Include(ws => ws.CreatedBy)
            .Include(ws => ws.Organization)
            .FirstOrDefaultAsync();

        if (subscription == null)
            throw new RecordNotFoundException("Webhook subscription not found.");

        return ServiceActionResult<WebhookSubscriptionItem>.OK(subscription.ToViewWithSecret());
    }

    public async Task<ServiceActionResult<WebhookSubscriptionItem>> CreateSubscriptionAsync(WebhookSubscriptionItem item)
    {
        using var l = log.TraceScope();

        var query = Repository.GetWebhookSubscriptionQuery(AuthorizationContext.CurrentProfile);
        var subscription = query.CreateNew();
        subscription.ToRecord(item);
        subscription.OrganizationId = await ResolveOrganizationIdAsync(item.OrganizationId);
        subscription.Secret = GenerateSecret();
        subscription.CreatedById = AuthorizationContext.CurrentProfile.Id;

        await query.AddAsync(subscription);
        l.I($"Webhook subscription created: {subscription.EventType} -> {subscription.Url}");

        var created = await LoadSubscriptionAsync(subscription.PublicId);
        return ServiceActionResult<WebhookSubscriptionItem>.OK(created.ToViewWithSecret());
    }

    public async Task<ServiceActionResult<WebhookSubscriptionItem>> UpdateSubscriptionAsync(string id, WebhookSubscriptionItem item)
    {
        using var l = log.TraceScope();

        var query = Repository.GetWebhookSubscriptionQuery(AuthorizationContext.CurrentProfile);
        var subscription = await query.ByPublicId(id).Select().FirstOrDefaultAsync();

        if (subscription == null)
            throw new RecordNotFoundException("Webhook subscription not found.");

        subscription.ToRecord(item);
        subscription.OrganizationId = await ResolveOrganizationIdAsync(item.OrganizationId);
        await query.UpdateAsync(subscription);
        l.I($"Webhook subscription updated: {subscription.EventType} -> {subscription.Url}");

        var updated = await LoadSubscriptionAsync(subscription.PublicId);
        return ServiceActionResult<WebhookSubscriptionItem>.OK(updated.ToViewWithSecret());
    }

    public async Task<ServiceActionResult<bool>> DeleteSubscriptionAsync(string id)
    {
        using var l = log.TraceScope();

        var query = Repository.GetWebhookSubscriptionQuery(AuthorizationContext.CurrentProfile);
        var subscription = await query.ByPublicId(id).Select().FirstOrDefaultAsync();

        if (subscription == null)
            throw new RecordNotFoundException("Webhook subscription not found.");

        await query.RemoveAsync(subscription);
        l.I($"Webhook subscription deleted: {subscription.EventType} -> {subscription.Url}");

        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<ModelList<WebhookDeliveryItem>>> GetDeliveriesAsync(
        int top, int page, string? subscriptionId = null, string[]? sortBy = null)
    {
        using var l = log.TraceScope();

        var query = Repository.GetWebhookDeliveryQuery(AuthorizationContext.CurrentProfile);

        if (!string.IsNullOrEmpty(subscriptionId))
        {
            var subscription = await Repository.GetWebhookSubscriptionQuery(AuthorizationContext.CurrentProfile)
                .ByPublicId(subscriptionId).Select().FirstOrDefaultAsync();

            if (subscription != null)
                query = query.BySubscriptionId(subscription.Id);
        }

        var sortField = sortBy?.FirstOrDefault()?.TrimStart('-');
        var isAsc = sortBy?.FirstOrDefault()?.StartsWith("-") != true;

        query = !string.IsNullOrEmpty(sortField)
            ? query.SortBy(sortField, isAsc)
            : query.SortBy("createdate", false);

        var totalCount = await query.Clone().Select().CountAsync();
        var deliveries = await query.Paginate(top, page).Select()
            .Include(wd => wd.Subscription)
            .ToListAsync();

        var items = deliveries.Select(wd => wd.ToView()).ToArray();

        var modelList = ModelListResult.CreateList(items, totalCount, top, page, sortBy, null);
        return ServiceActionResult<ModelList<WebhookDeliveryItem>>.OK(modelList);
    }

    private static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    private async Task<Guid?> ResolveOrganizationIdAsync(string? organizationPublicId)
    {
        if (string.IsNullOrWhiteSpace(organizationPublicId))
            return null;

        var organization = await Repository.GetOrganizationsQuery(AuthorizationContext.CurrentProfile)
            .ByPublicId(organizationPublicId)
            .Select()
            .FirstOrDefaultAsync();

        if (organization == null)
            throw new RecordNotFoundException("Organization not found.");

        return organization.Id;
    }

    private async Task<Database.Core.Models.Webhooks.WebhookSubscription> LoadSubscriptionAsync(string publicId)
    {
        var subscription = await Repository.GetWebhookSubscriptionQuery(AuthorizationContext.CurrentProfile)
            .ByPublicId(publicId)
            .Select()
            .Include(ws => ws.CreatedBy)
            .Include(ws => ws.Organization)
            .FirstAsync();

        return subscription;
    }
}
