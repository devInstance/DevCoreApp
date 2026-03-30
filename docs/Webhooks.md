# Webhooks

## Overview

The webhook feature provides outbound event delivery from the application to external systems. In the current starter implementation, it supports:

- webhook subscription management in the admin UI
- event-based subscription matching
- asynchronous delivery through the background worker
- HMAC-SHA256 request signing
- delivery attempt logging
- retry metadata and delivery status tracking

This is an outbound webhook system. The starter does not currently include inbound webhook receiver endpoints.

## Why This Feature Exists

Webhook support is included so new projects can notify other systems when important business events occur, without forcing those integrations to poll the application database or API.

Typical use cases include:

- syncing users or organizations into another platform
- notifying internal systems about lifecycle events
- triggering downstream workflows
- posting updates into third-party automation tools

## High-Level Design

The webhook feature is split into four main parts:

| Area | Responsibility | Main files |
|---|---|---|
| Subscription management | Stores and manages webhook endpoints | `src/Server/Admin/Services/Webhooks/WebhookAdminService.cs` |
| Event dispatching | Finds matching subscriptions and creates delivery jobs | `src/Server/Admin/Services/Webhooks/WebhookDispatcher.cs` |
| Background delivery | Sends HTTP requests and updates delivery status | `src/Server/Admin/Services/Background/Tasks/Handlers/WebhookDeliveryTaskHandler.cs` |
| Admin visibility | Lets admins manage subscriptions and inspect delivery logs | `src/Server/Admin/WebService/UI/Pages/Admin/WebhooksPage.razor` |

The flow is:

1. Application code calls `IWebhookDispatcher.DispatchAsync(eventType, payload)`.
2. The dispatcher finds all active subscriptions for that event type.
3. A `WebhookDelivery` record is created for each subscription.
4. A background task is queued for each delivery.
5. The background worker runs `WebhookDeliveryTaskHandler`.
6. The handler signs and POSTs the payload to the subscription URL.
7. The delivery record is updated with status, response details, and retry metadata.

## Data Model

### WebhookSubscription

`WebhookSubscription` stores registered endpoints.

```csharp
public class WebhookSubscription : DatabaseObject
{
    public string EventType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
    public Guid CreatedById { get; set; }

    public UserProfile CreatedBy { get; set; } = default!;
    public Organization? Organization { get; set; }
}
```

#### Field meanings

| Field | Type | Meaning |
|---|---|---|
| `EventType` | `string` | Logical event key such as `User.Created`. |
| `Url` | `string` | Destination endpoint for delivery. |
| `Secret` | `string` | Per-subscription secret used to sign requests. |
| `OrganizationId` | `Guid?` | Optional organization link in the data model. |
| `CreatedById` | `Guid` | Internal user profile id of the creator. |

### WebhookDelivery

`WebhookDelivery` stores each delivery attempt record.

```csharp
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
```

#### Field meanings

| Field | Type | Meaning |
|---|---|---|
| `SubscriptionId` | `Guid` | Internal FK to the subscription. |
| `EventType` | `string` | Event that triggered the delivery. |
| `Payload` | `string` | JSON payload sent to the endpoint. |
| `ResponseStatusCode` | `int?` | HTTP response code from the remote endpoint. |
| `ResponseBody` | `string?` | Response content or failure message. |
| `AttemptCount` | `int` | Number of delivery attempts made so far. |
| `NextRetryAt` | `DateTime?` | Next planned retry time, when applicable. |
| `Status` | `WebhookDeliveryStatus` | `Pending`, `Delivered`, or `Failed`. |

## EF Core Configuration

The database configuration lives in `ApplicationDbContext`.

### Subscription configuration

- `CreatedById` points to `UserProfile` with `DeleteBehavior.Restrict`
- `OrganizationId` points to `Organization` with `DeleteBehavior.Cascade`
- index on `EventType`
- index on `(EventType, IsActive)`

### Delivery configuration

- `Payload` is stored as `jsonb`
- `SubscriptionId` points to `WebhookSubscription` with `DeleteBehavior.Cascade`
- indexes on:
  - `SubscriptionId`
  - `Status`
  - `EventType`
  - `NextRetryAt`

### Implications

- deleting a subscription also deletes its delivery log
- event-type lookup is optimized
- pending and retry-oriented monitoring is supported by indexes

## Shared Models and Validation

### WebhookSubscriptionItem

This is the admin/service DTO for subscriptions.

Validation rules:

| Field | Rules |
|---|---|
| `EventType` | required, min length `2`, max length `256` |
| `Url` | required, valid URL, max length `2048` |

The DTO also exposes:

- `IsActive`
- `CreatedByName`
- `OrganizationName`
- `CreateDate`
- `UpdateDate`

### WebhookDeliveryItem

This is the admin/service DTO for delivery log entries. It includes:

- subscription public id
- event type
- resolved destination URL
- JSON payload
- HTTP status
- response body
- attempt count
- next retry time
- delivery status
- timestamps

## Supported Event Types

The starter defines a small fixed set of event names in `WebhookEventTypes`.

| Constant | Value |
|---|---|
| `UserCreated` | `User.Created` |
| `UserUpdated` | `User.Updated` |
| `UserDeleted` | `User.Deleted` |
| `OrganizationCreated` | `Organization.Created` |
| `OrganizationUpdated` | `Organization.Updated` |

These are exposed to the admin UI through `WebhookEventTypes.All`.

## Subscription Management

Subscription CRUD is handled by `IWebhookAdminService` and `WebhookAdminService`.

### Supported admin operations

- list subscriptions
- search by event type or URL
- sort by event type, URL, or create date
- create subscriptions
- update subscriptions
- delete subscriptions
- view delivery log
- view delivery log filtered by subscription

### Create behavior

When a subscription is created:

- the submitted DTO is mapped into a new `WebhookSubscription`
- a new secret is generated with 32 random bytes and Base64 encoding
- `CreatedById` is set from `AuthorizationContext.CurrentProfile.Id`

The generated secret is stored in the database and used for request signing.

### Update behavior

Updates modify:

- `EventType`
- `Url`
- `IsActive`

The secret is not rotated during update.

### Delete behavior

Deleting a subscription removes the subscription and, because of FK cascade delete, also removes all associated delivery rows.

## Admin UI

### Subscriptions page

The subscription management page is:

```text
/admin/webhooks
```

The page supports:

- search
- paging
- sorting
- grid settings persistence
- create/edit modal
- delete confirmation
- link to delivery log

The create/edit modal currently exposes:

- `EventType`
- `Url`
- `IsActive`

The page is protected by:

```text
Admin.Webhooks.View
```

The create, edit, and delete buttons are additionally wrapped in `AuthorizeView` with:

- `Admin.Webhooks.Create`
- `Admin.Webhooks.Edit`
- `Admin.Webhooks.Delete`

### Delivery log page

The delivery log pages are:

```text
/admin/webhook-deliveries
/admin/webhook-deliveries/{SubscriptionId}
```

The page supports:

- global delivery list
- per-subscription filtered list
- paging
- sorting
- grid settings persistence
- row click to open delivery details

The detail modal shows:

- event type
- URL
- status
- HTTP status
- attempts
- created timestamp
- request payload
- response body

## Dispatch Flow

Runtime dispatching is handled by `IWebhookDispatcher`.

```csharp
public interface IWebhookDispatcher
{
    Task DispatchAsync(string eventType, object eventPayload);
}
```

### Dispatch behavior

When `DispatchAsync` is called:

1. active subscriptions for the event type are loaded
2. if none exist, the method logs and returns
3. the payload object is JSON-serialized using its runtime type
4. one `WebhookDelivery` row is created per matching subscription
5. one background task is queued per delivery

Each background job payload is a `WebhookDeliveryRequest` containing:

- delivery public id
- subscription public id
- event type
- serialized payload

### Integration pattern

Projects using this starter are expected to call `IWebhookDispatcher` from application services when domain-relevant events occur.

Example:

```csharp
await _webhookDispatcher.DispatchAsync(
    WebhookEventTypes.UserCreated,
    new
    {
        userId = user.PublicId,
        email = user.Email
    });
```

The current starter provides the dispatcher and background delivery pipeline, but it does not yet wire event dispatch into existing admin CRUD flows automatically.

## Delivery Execution

Actual HTTP delivery is performed by `WebhookDeliveryTaskHandler`.

### Request construction

The handler sends an HTTP `POST` to the subscription URL with:

- `Content-Type: application/json`
- request body = serialized JSON payload
- `X-Webhook-Event` header = event type
- `X-Webhook-Delivery` header = delivery public id
- `X-Webhook-Signature` header = `sha256={hex digest}`

### Signature generation

The signature is:

- HMAC-SHA256
- key = subscription secret as UTF-8 bytes
- message = raw payload JSON as UTF-8 bytes
- output = lowercase hex string

This allows receivers to verify authenticity if they know the shared secret.

### Success behavior

If the remote endpoint returns a success status code:

- `ResponseStatusCode` is set
- `ResponseBody` is captured
- response body is truncated to 4096 characters if needed
- delivery status becomes `Delivered`
- `NextRetryAt` is cleared

### Failure behavior

If the remote endpoint returns a non-success response or throws `HttpRequestException` or `TaskCanceledException`:

- `AttemptCount` is incremented
- `ResponseStatusCode` or `ResponseBody` is captured when available
- retry metadata may be set
- the handler throws after update when the delivery should be retried

## Retry Behavior

Retry behavior currently spans two different layers:

- `WebhookDeliveryTaskHandler`
- background task infrastructure

### Handler-level retry metadata

Inside the delivery handler:

- max delivery attempts is defined as `5`
- backoff schedule is `30s`, `120s`, `480s`, `1920s`
- `NextRetryAt` is set on the delivery record
- delivery stays `Pending` until max attempts is reached

### Background task retry behavior

Inside `BackgroundWorker.SubmitAsync`, webhook background tasks are created with:

- `MaxRetries = 3`

Inside `BackgroundTaskWorker`:

- task retries use worker-level exponential backoff
- the worker re-queues the task when the handler throws
- once retry count reaches the task max, the background task is marked `Failed`

### Important mismatch

These two retry systems are not aligned.

Practical result:

- the delivery handler plans for up to 5 attempts
- the background task only retries 3 times
- on the final worker retry failure, the delivery record can remain `Pending` with a future `NextRetryAt`
- no additional retry may actually happen, because the background task has already failed

This is a real implementation gap in the current starter.

## Query and Mapping Behavior

### Subscription query capabilities

`CoreWebhookSubscriptionQuery` supports:

- `ByPublicId`
- `ByEventType`
- `ActiveOnly`
- `Search`
- paging
- sort by:
  - `eventtype`
  - `url`
  - `createdate`

### Delivery query capabilities

`CoreWebhookDeliveryQuery` supports:

- `ByPublicId`
- `BySubscriptionId`
- `ByStatus`
- `ByEventType`
- paging
- sort by:
  - `eventtype`
  - `status`
  - `createdate`

### Mapping behavior

Subscription mapping exposes:

- `PublicId` as DTO `Id`
- creator display name
- organization display name

Delivery mapping exposes:

- `Subscription.PublicId` as DTO `SubscriptionId`
- `Subscription.Url` as DTO `Url`
- stored payload and response details

## Permissions and Access Control

Webhook permissions are defined in `PermissionDefinitions`.

| Permission | Purpose |
|---|---|
| `Admin.Webhooks.View` | View subscriptions and deliveries |
| `Admin.Webhooks.Create` | Create subscriptions |
| `Admin.Webhooks.Edit` | Edit subscriptions |
| `Admin.Webhooks.Delete` | Delete subscriptions |

### Current enforcement state

- the subscriptions page requires `Admin.Webhooks.View`
- the deliveries page requires `Admin.Webhooks.View`
- subscription create/edit/delete buttons are hidden in the UI based on the fine-grained permission policies
- the service methods themselves do not currently show explicit per-action permission checks in `WebhookAdminService`

This means UI enforcement is stronger here than in feature flags, but service-layer enforcement still depends on broader application authorization patterns.

## Mock Mode

The starter includes `WebhookAdminServiceMock`.

It provides sample:

- subscriptions
- delivered records
- failed records
- pending retry records

This is useful for UI development and demo data, but it only simulates admin CRUD and delivery-log viewing. It does not simulate actual dispatching, signing, or HTTP delivery.

## Current Limitations and Gaps

### 1. No inbound webhook receiver support

The starter only implements outbound webhook delivery. There are no provider-specific inbound endpoints, signature validators, or receiver controllers for third-party callbacks.

### 2. Secret is generated but not surfaced to admins

A secret is generated on create and stored in the database, but:

- it is not returned in `WebhookSubscriptionItem`
- it is not shown in the admin UI
- there is no rotate-secret or reveal-secret workflow

Practical result:

- the receiver cannot easily configure signature verification unless the secret is retrieved another way

### 3. Organization support is only partial

`WebhookSubscription` includes `OrganizationId`, but the current DTO and UI do not expose it, and `WebhookSubscription` does not implement `IOrganizationScoped`.

Practical result:

- organization linkage exists in the schema
- the built-in UI does not let admins assign it
- automatic organization query filtering does not apply to this entity

### 4. Dispatcher is infrastructure, not full domain integration

The dispatcher is available, but the starter does not automatically emit events from existing feature flows. Each project still needs to decide where to call `IWebhookDispatcher`.

### 5. Retry state can become misleading

Because delivery retries and background task retries use different limits and schedulers, a delivery may show:

- `Pending`
- a future `NextRetryAt`

even when the underlying background task has already exhausted its retries and failed.

### 6. No manual replay or retry action in admin UI

The delivery log is view-only. There is no built-in button to:

- retry a failed delivery
- replay a delivered webhook
- resend with a regenerated payload

### 7. Deleting a subscription removes historical delivery logs

This follows the current FK cascade behavior, but it may not be what every project wants for auditability.

## Recommended Next Improvements

If you want to make this webhook starter more production-ready, the highest-value follow-ups are:

1. Add a secure secret reveal and rotation workflow.
2. Align delivery retry logic with background task retry logic.
3. Add manual replay/retry actions in the admin UI.
4. Decide whether delivery logs should be preserved after subscription deletion.
5. Add organization assignment and enforce organization scoping consistently.
6. Wire `IWebhookDispatcher` into the business events your project actually emits.
7. Add tests for signature generation, retry behavior, and failure transitions.

## File Reference Map

| Concern | File |
|---|---|
| Admin service | `src/Server/Admin/Services/Webhooks/WebhookAdminService.cs` |
| Dispatcher | `src/Server/Admin/Services/Webhooks/WebhookDispatcher.cs` |
| Dispatcher interface | `src/Server/Admin/Services/Webhooks/IWebhookDispatcher.cs` |
| Background delivery handler | `src/Server/Admin/Services/Background/Tasks/Handlers/WebhookDeliveryTaskHandler.cs` |
| Delivery request payload | `src/Server/Admin/Services/Background/Requests/WebhookDeliveryRequest.cs` |
| Background task types | `src/Server/Admin/Services/Background/Tasks/BackgroundTaskTypes.cs` |
| Subscription entity | `src/Server/Database/Core/Models/Webhooks/WebhookSubscription.cs` |
| Delivery entity | `src/Server/Database/Core/Models/Webhooks/WebhookDelivery.cs` |
| Subscription DTO | `src/Shared/Model/Webhooks/WebhookSubscriptionItem.cs` |
| Delivery DTO | `src/Shared/Model/Webhooks/WebhookDeliveryItem.cs` |
| Event constants | `src/Shared/Model/Webhooks/WebhookEventTypes.cs` |
| Delivery status enum | `src/Shared/Model/Webhooks/WebhookDeliveryStatus.cs` |
| EF configuration | `src/Server/Database/Core/ApplicationDbContext.cs` |
| Subscription query | `src/Server/Database/Core/Data/Queries/BasicsImplementation/CoreWebhookSubscriptionQuery.cs` |
| Delivery query | `src/Server/Database/Core/Data/Queries/BasicsImplementation/CoreWebhookDeliveryQuery.cs` |
| Subscription mapping | `src/Server/Database/Core/Data/Decorators/WebhookSubscriptionDecorators.cs` |
| Delivery mapping | `src/Server/Database/Core/Data/Decorators/WebhookDeliveryDecorators.cs` |
| Admin subscriptions page | `src/Server/Admin/WebService/UI/Pages/Admin/WebhooksPage.razor` |
| Admin deliveries page | `src/Server/Admin/WebService/UI/Pages/Admin/WebhookDeliveriesPage.razor` |
| Mock service | `mocks/Server/Admin/ServicesMocks/Webhooks/WebhookAdminServiceMock.cs` |

