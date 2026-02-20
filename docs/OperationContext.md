# IOperationContext

## Overview

`IOperationContext` is an abstraction that provides contextual information about the current operation — whether it's an HTTP request or a background job — to the data layer. It lives in `Database.Core` so that EF Core interceptors, global query filters, and repositories can consume it without depending on ASP.NET Core HTTP types.

## Why It Exists

Several cross-cutting concerns in the data layer need to know **who** is performing an operation and **what scope** they have access to:

- **Audit logging** — The `SaveChangesInterceptor` needs the current user's ID to record who created or modified a record.
- **Organization scoping** — EF Core global query filters will use `VisibleOrganizationIds` to restrict which rows a query returns, enforcing data isolation.
- **Correlation/tracing** — A `CorrelationId` ties together log entries across services for a single logical operation.

The data layer (`Database.Core`) cannot reference ASP.NET Core or `IHttpContextAccessor` directly — that would violate the dependency rules. `IOperationContext` provides the information these features need through a clean, HTTP-independent interface.

## Interface

```
Database.Core/Data/IOperationContext.cs
```

| Property | Type | Description |
|---|---|---|
| `UserId` | `Guid?` | Internal PK of the current `UserProfile`. Not `ApplicationUser.Id`. |
| `PrimaryOrganizationId` | `Guid?` | The organization new records should be assigned to. |
| `VisibleOrganizationIds` | `IReadOnlySet<Guid>` | Organizations the user can query. Used by EF global filters. |
| `IpAddress` | `string?` | Caller's IP address (for audit trails). |
| `CorrelationId` | `string?` | Unique trace identifier for the operation. |

## Implementations

### HttpOperationContext

**Location:** `Admin.WebService/Identity/HttpOperationContext.cs`

Used during HTTP requests (Blazor SSR pages, API controllers). Registered as `Scoped`.

| Property | Source |
|---|---|
| `UserId` | Lazy: `ClaimsPrincipal` → `UserManager.GetUserId()` → `UserProfilesQuery.ByApplicationUserId()` → `UserProfile.Id` |
| `PrimaryOrganizationId` | `null` (not yet implemented) |
| `VisibleOrganizationIds` | Empty set (not yet implemented) |
| `IpAddress` | `HttpContext.Connection.RemoteIpAddress` |
| `CorrelationId` | `Activity.Current?.Id` ?? `HttpContext.TraceIdentifier` |

**Lazy resolution:** The `UserId` property only queries the database on first access. The result is cached for the rest of the scoped lifetime. This follows the same pattern as `AuthorizationContext.CurrentProfile`.

### BackgroundOperationContext

**Location:** `Admin.Services/Background/BackgroundOperationContext.cs`

Used during background job execution. All properties have public setters so the `BackgroundWorker` can populate them from job metadata before processing each job.

**Additional methods:**
- `SetVisibleOrganizationIds(IEnumerable<Guid>)` — Replaces the read-only set.
- `Reset()` — Clears all properties. Called at the start of each job to prevent state leaking between jobs.

## DI Registration

In `Program.cs`, a factory lambda automatically selects the correct implementation:

```csharp
builder.Services.AddScoped<HttpOperationContext>();
builder.Services.AddScoped<BackgroundOperationContext>();
builder.Services.AddScoped<IOperationContext>(sp =>
{
    var accessor = sp.GetRequiredService<IHttpContextAccessor>();
    return accessor.HttpContext != null
        ? sp.GetRequiredService<HttpOperationContext>()
        : sp.GetRequiredService<BackgroundOperationContext>();
});
```

When `HttpContext` is present (HTTP request pipeline), the factory returns `HttpOperationContext`. When it's absent (background worker, which doesn't have an HTTP context), it returns `BackgroundOperationContext`.

## BackgroundWorker Scope-Per-Job

As part of this change, `BackgroundWorker.ExecuteAsync()` was refactored from a single long-lived DI scope to creating a **new scope per job**:

```
Before: one scope for the entire application lifetime
  └── DbContext accumulates tracked entities over time
  └── No way to set per-job context

After: fresh scope per job
  └── DbContext is disposed after each job (clean tracking state)
  └── BackgroundOperationContext is reset and can be populated per job
```

This prevents memory growth from entity tracking and enables future per-job context propagation from `BackgroundRequestItem` metadata.

## Relationship to IAuthorizationContext

`IOperationContext` and `IAuthorizationContext` coexist and serve different purposes:

| | `IAuthorizationContext` | `IOperationContext` |
|---|---|---|
| **Layer** | Services (Admin.Services) | Data (Database.Core) |
| **Returns** | Full `UserProfile` entity, `ClaimsPrincipal` | Primitive IDs only (Guid, string) |
| **Used by** | Business logic, authorization checks | EF interceptors, query filters |
| **HTTP dependency** | Yes (references `IHttpContextAccessor`) | No (interface is HTTP-independent) |

Services that need the full user profile or claims should continue using `IAuthorizationContext`. The data layer should use `IOperationContext` for the subset of information it needs.

## Future Work

When the **organization hierarchy** feature is implemented:

1. `HttpOperationContext` will query `UserOrganizations` to resolve `PrimaryOrganizationId` and `VisibleOrganizationIds` based on the user's org memberships and scope (Self vs. WithChildren).
2. `BackgroundWorker` will populate `BackgroundOperationContext` org fields from job metadata (the submitting user's org context at the time the job was queued).
3. EF Core global query filters will read `VisibleOrganizationIds` from `IOperationContext` to enforce row-level data isolation.
