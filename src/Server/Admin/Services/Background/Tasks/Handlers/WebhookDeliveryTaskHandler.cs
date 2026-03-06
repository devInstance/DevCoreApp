using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevInstance.DevCoreApp.Server.Admin.Services.Background.Requests;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Shared.Model.Webhooks;
using DevInstance.LogScope;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Background.Tasks.Handlers;

public class WebhookDeliveryTaskHandler : IBackgroundTaskHandler
{
    public string TaskType => BackgroundTaskTypes.DeliverWebhook;

    private readonly IScopeLog log;

    public WebhookDeliveryTaskHandler(IScopeManager logManager)
    {
        log = logManager.CreateLogger(this);
    }

    public async Task HandleAsync(string payload, IServiceProvider scopedProvider, CancellationToken cancellationToken)
    {
        using var l = log.TraceScope();

        var request = JsonSerializer.Deserialize<WebhookDeliveryRequest>(payload)
            ?? throw new InvalidOperationException("Failed to deserialize webhook delivery request payload.");

        if (string.IsNullOrEmpty(request.DeliveryId))
            throw new InvalidOperationException("DeliveryId is required.");

        var repository = scopedProvider.GetRequiredService<IQueryRepository>();

        // Load the delivery record
        var deliveryQuery = repository.GetWebhookDeliveryQuery(null!);
        var delivery = await deliveryQuery.ByPublicId(request.DeliveryId).Select()
            .FirstOrDefaultAsync(cancellationToken);

        if (delivery == null)
        {
            l.E($"Webhook delivery record {request.DeliveryId} not found.");
            return;
        }

        // Load the subscription to get URL and secret
        var subscriptionQuery = repository.GetWebhookSubscriptionQuery(null!);
        var subscription = await subscriptionQuery.ByPublicId(request.SubscriptionPublicId).Select()
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription == null)
        {
            l.E($"Webhook subscription {request.SubscriptionPublicId} not found. Marking delivery as failed.");
            delivery.Status = WebhookDeliveryStatus.Failed;
            delivery.ResponseBody = "Subscription not found.";
            var failQuery = repository.GetWebhookDeliveryQuery(null!);
            await failQuery.UpdateAsync(delivery);
            return;
        }

        if (!subscription.IsActive)
        {
            l.I($"Webhook subscription {subscription.PublicId} is inactive. Marking delivery as failed.");
            delivery.Status = WebhookDeliveryStatus.Failed;
            delivery.ResponseBody = "Subscription is inactive.";
            var failQuery = repository.GetWebhookDeliveryQuery(null!);
            await failQuery.UpdateAsync(delivery);
            return;
        }

        delivery.AttemptCount++;

        try
        {
            var httpClientFactory = scopedProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("WebhookDelivery");

            // Build HMAC-SHA256 signature
            var signature = ComputeHmacSignature(request.Payload, subscription.Secret);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, subscription.Url);
            httpRequest.Content = new StringContent(request.Payload, Encoding.UTF8, "application/json");
            httpRequest.Headers.Add("X-Webhook-Event", request.EventType);
            httpRequest.Headers.Add("X-Webhook-Signature", $"sha256={signature}");
            httpRequest.Headers.Add("X-Webhook-Delivery", delivery.PublicId);

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);

            delivery.ResponseStatusCode = (int)response.StatusCode;
            delivery.ResponseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            // Truncate response body to prevent bloat
            if (delivery.ResponseBody?.Length > 4096)
                delivery.ResponseBody = delivery.ResponseBody[..4096];

            if (response.IsSuccessStatusCode)
            {
                delivery.Status = WebhookDeliveryStatus.Delivered;
                delivery.NextRetryAt = null;
                l.I($"Webhook delivered successfully to {subscription.Url} (HTTP {delivery.ResponseStatusCode}).");
            }
            else
            {
                HandleRetry(delivery);
                l.E($"Webhook delivery to {subscription.Url} failed with HTTP {delivery.ResponseStatusCode}.");
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            delivery.ResponseBody = ex.Message;
            HandleRetry(delivery);
            l.E($"Webhook delivery to {subscription.Url} failed: {ex.Message}");
        }

        var updateQuery = repository.GetWebhookDeliveryQuery(null!);
        await updateQuery.UpdateAsync(delivery);

        // If we need to retry, throw so the background worker re-queues
        if (delivery.Status == WebhookDeliveryStatus.Pending && delivery.NextRetryAt.HasValue)
        {
            throw new InvalidOperationException(
                $"Webhook delivery failed (attempt {delivery.AttemptCount}). Retry scheduled at {delivery.NextRetryAt}.");
        }
    }

    private static void HandleRetry(Database.Core.Models.Webhooks.WebhookDelivery delivery)
    {
        const int maxAttempts = 5;

        if (delivery.AttemptCount >= maxAttempts)
        {
            delivery.Status = WebhookDeliveryStatus.Failed;
            delivery.NextRetryAt = null;
        }
        else
        {
            // Exponential backoff: 30s, 120s, 480s, 1920s
            var delaySeconds = 30 * Math.Pow(4, delivery.AttemptCount - 1);
            delivery.NextRetryAt = DateTime.UtcNow.AddSeconds(Math.Min(delaySeconds, 7200));
        }
    }

    private static string ComputeHmacSignature(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(keyBytes, payloadBytes);
        return Convert.ToHexStringLower(hash);
    }
}
