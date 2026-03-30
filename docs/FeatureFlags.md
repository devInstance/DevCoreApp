# Feature Flags

## Overview

The feature flag system provides a built-in way to gate functionality without redeploying the application. In the current starter implementation, feature flags support:

- global on/off toggles
- optional organization-scoped overrides at the data level
- optional percentage rollout for global flags
- optional user allowlists
- an admin UI for CRUD operations
- a runtime evaluation service for application code

The implementation is split across the database model, query layer, admin CRUD service, admin UI, and a dedicated evaluation service.

## Why This Feature Exists

This starter includes feature flags so new projects can:

- release features gradually
- hide incomplete work in production
- enable beta access for selected users
- turn off unstable features quickly
- keep rollout logic centralized instead of scattering hard-coded conditions through the codebase

## High-Level Design

The feature flag stack is composed of these parts:

| Area | Responsibility | Main files |
|---|---|---|
| Data model | Stores flag definitions and rollout metadata | `src/Server/Database/Core/Models/FeatureFlag.cs` |
| EF configuration | Configures indexes, relationships, and JSON storage | `src/Server/Database/Core/ApplicationDbContext.cs` |
| Query layer | Filtering, search, sort, pagination | `src/Server/Database/Core/Data/Queries/BasicsImplementation/CoreFeatureFlagQuery.cs` |
| Decorators | Maps DB entity to shared UI/service model | `src/Server/Database/Core/Data/Decorators/FeatureFlagDecorators.cs` |
| Shared DTO | Validation rules for create/edit operations | `src/Shared/Model/FeatureFlags/FeatureFlagItem.cs` |
| Admin CRUD service | Create, read, update, delete flag records | `src/Server/Admin/Services/FeatureFlags/FeatureFlagAdminService.cs` |
| Runtime evaluation service | Determines whether a feature is enabled for the current context | `src/Server/Admin/Services/FeatureFlags/FeatureFlagService.cs` |
| Admin UI | Displays and edits flags in the back office | `src/Server/Admin/WebService/UI/Pages/Admin/FeatureFlagsPage.razor` |
| Mock service | Enables UI development without a database | `mocks/Server/Admin/ServicesMocks/FeatureFlags/FeatureFlagAdminServiceMock.cs` |

## Data Model

The persisted entity is `FeatureFlag`.

```csharp
public class FeatureFlag : DatabaseObject
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
    public Guid? OrganizationId { get; set; }
    public int? RolloutPercentage { get; set; }
    public List<string>? AllowedUsers { get; set; }

    public Organization? Organization { get; set; }
}
```

### Field meanings

| Field | Type | Meaning |
|---|---|---|
| `Name` | `string` | Logical flag key used by application code. |
| `Description` | `string?` | Human-readable explanation for admins. |
| `IsEnabled` | `bool` | Base enabled/disabled state for the record. |
| `OrganizationId` | `Guid?` | `null` means global flag. A value means organization-specific override. |
| `RolloutPercentage` | `int?` | Optional global rollout bucket from `0` to `100`. |
| `AllowedUsers` | `List<string>?` | Optional list of users who should always pass evaluation. |

### EF Core configuration

The database configuration is defined in `ApplicationDbContext`:

- `AllowedUsers` is stored as `jsonb`
- `OrganizationId` is an optional foreign key to `Organization`
- unique index on `(Name, OrganizationId)`
- non-unique index on `Name`

This allows:

- one global flag per name
- one override per organization per name
- fast lookup by flag name during evaluation

## Shared Model and Validation

The admin/service DTO is `FeatureFlagItem`.

### Validation rules

| Field | Rules |
|---|---|
| `Name` | required, min length `2`, max length `256` |
| `Description` | max length `500` |
| `RolloutPercentage` | range `0` to `100` |

The shared model exposes `OrganizationName` for display, but it does not expose `OrganizationId`. That matters because it limits what the current admin UI can create or update, described later in the limitations section.

## Runtime Evaluation

Runtime evaluation is implemented by `IFeatureFlagService` and `FeatureFlagService`.

```csharp
public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string name);
    void InvalidateCache(string name);
}
```

### Evaluation algorithm

`IsEnabledAsync(string name)` currently evaluates in this order:

1. Load all records with the matching name.
2. If no records exist, return `false`.
3. If the current user appears in any `AllowedUsers` list, return `true`.
4. If the current request has a `PrimaryOrganizationId` and an organization-specific flag exists for that organization, return that flag's `IsEnabled` value.
5. Otherwise look for the global flag.
6. If there is no global flag, return `false`.
7. If the global flag has no rollout percentage, or rollout is `>= 100`, return `globalFlag.IsEnabled`.
8. If rollout is `<= 0`, return `false`.
9. If rollout is between `1` and `99`, evaluate a stable per-user hash bucket and return whether the user falls inside the percentage.

### Important behavior details

#### 1. User allowlist has highest priority

If the current user matches `AllowedUsers`, the service returns `true` immediately. This bypasses:

- global `IsEnabled = false`
- organization overrides
- rollout percentage checks

That makes the allowlist a true force-enable mechanism.

#### 2. Organization override beats global record

If the request has a resolved `PrimaryOrganizationId`, an organization-specific record is checked before the global record. That allows per-organization enable or disable behavior.

#### 3. Percentage rollout only applies to the global flag

Rollout logic is only evaluated after organization override lookup and only against the global record. There is no organization-specific rollout evaluation in the current implementation.

#### 4. Percentage rollout requires a resolved user id

For rollout values between `1` and `99`, the service requires `IOperationContext.UserId`. If no current user id is available, evaluation returns `false`.

#### 5. Rollout ignores `IsEnabled` when percentage is between `1` and `99`

For partial rollout, the current code returns the bucket check result directly and does not combine it with `globalFlag.IsEnabled`. That means a global record with:

- `IsEnabled = false`
- `RolloutPercentage = 25`

will still evaluate to `true` for roughly 25% of users.

This is current behavior and should be treated as implementation-specific, not assumed business intent.

### Stable bucketing

Partial rollout uses a SHA-256 hash of:

```text
{currentUserId}{featureName}
```

The first 4 bytes are converted into an integer bucket `0-99`. This gives a consistent result for the same user and flag name across requests, which is what you want for gradual rollout.

## Caching

`FeatureFlagService` caches lookup results in `IMemoryCache` for 5 minutes.

### Cache characteristics

- cache key format: `featureflag:{name}`
- cache granularity: one cache entry per flag name
- cached value: full list of records for that name
- duration: 5 minutes

### Why this matters

The service does not query the database every time a feature is checked. This reduces DB overhead for high-traffic flags, but it also means updates are not visible immediately unless the cache entry is removed.

### Current limitation

`FeatureFlagService` exposes `InvalidateCache(string name)`, but `FeatureFlagAdminService` does not call it after create, update, or delete. As a result:

- admin changes may not take effect immediately
- runtime evaluation can continue using stale data for up to 5 minutes

If immediate consistency is required, cache invalidation should be wired into the admin write path.

## Admin Feature Management

The admin page is available at:

```text
/admin/feature-flags
```

### UI capabilities

The page currently supports:

- list flags
- search by name or description
- paging
- sorting by `name` and `isenabled`
- per-user grid settings persistence
- create new flags
- edit existing flags
- delete flags

### UI fields

The modal editor currently exposes:

- `Name`
- `Description`
- `Enabled`
- `Rollout Percentage`
- `Allowed Users`

The grid displays:

- Name
- Description
- Scope
- Enabled
- Rollout
- Actions

### Grid profile support

The page uses `GridProfileService` with grid name `AdminFeatureFlags`, so page size, sorting, and visible columns are persisted per user.

## Permissions and Access Control

Feature flag permissions are defined in `PermissionDefinitions`.

| Permission | Purpose |
|---|---|
| `Admin.FeatureFlags.View` | Access the feature flags page |
| `Admin.FeatureFlags.Create` | Intended create permission |
| `Admin.FeatureFlags.Edit` | Intended edit permission |
| `Admin.FeatureFlags.Delete` | Intended delete permission |

### Current enforcement state

- The page itself is protected with `[Authorize(Policy = "Admin.FeatureFlags.View")]`.
- The feature flags menu item is shown inside the broader `Owner,Admin` navigation block.
- The admin service methods do not currently perform per-action permission checks for create, edit, or delete.
- The page does not hide create/edit/delete controls based on those finer-grained permission keys.

So the permission definitions exist, but enforcement is currently coarse at the page-access level.

## Query and CRUD Behavior

`FeatureFlagAdminService` uses the query repository and standard starter patterns for CRUD.

### Supported list operations

`GetFlagsAsync` supports:

- pagination
- free-text search on `Name` and `Description`
- sorting by:
  - `name`
  - `isenabled`
  - `createdate`

### Mapping behavior

`FeatureFlagDecorators` maps:

- DB `PublicId` to DTO `Id`
- `Organization?.Name` to `OrganizationName`
- rollout and allowlist values directly

### Current write behavior

`ToRecord()` writes:

- `Name`
- `Description`
- `IsEnabled`
- `RolloutPercentage`
- `AllowedUsers`

It does not write `OrganizationId`.

This means the current admin create/edit path cannot assign or modify organization-scoped flags even though the database model supports them.

## Mock Mode

The starter includes `FeatureFlagAdminServiceMock` for UI development and mock-hosted scenarios.

The mock service includes sample data for:

- global flags
- organization-named flags
- rollout percentages
- allowlisted users

This is useful for front-end work, but it is only for admin CRUD simulation. Runtime evaluation in `FeatureFlagService` behaves differently in mock mode:

- if no repository or cache is present, `IsEnabledAsync` returns `false`

So mock mode does not simulate real runtime flag evaluation.

## How Application Code Should Use It

Application code should depend on `IFeatureFlagService`, not query the database directly.

Example:

```csharp
public class SomeService
{
    private readonly IFeatureFlagService _featureFlags;

    public SomeService(IFeatureFlagService featureFlags)
    {
        _featureFlags = featureFlags;
    }

    public async Task DoWorkAsync()
    {
        if (await _featureFlags.IsEnabledAsync("NewDashboard"))
        {
            // new behavior
        }
        else
        {
            // existing behavior
        }
    }
}
```

### Recommended usage rules

- use stable, descriptive names such as `NewDashboard` or `BulkExport`
- keep all rollout logic behind `IFeatureFlagService`
- avoid duplicating feature-name strings throughout the codebase without central constants
- use flags to gate behavior, not to model long-term application configuration

## Current Limitations and Gaps

This section describes what the starter supports today versus what it appears to be designed to support later.

### 1. Organization scope is only partially implemented

The database model and evaluation service support organization-specific flags, but the current admin write path does not expose or persist `OrganizationId`.

Practical result:

- you can store organization-specific flags directly in the database
- the runtime service can evaluate them
- the built-in admin UI cannot create or edit them properly yet

### 2. Allowed-users label is misleading

The editor label says:

```text
Allowed Users (comma-separated PublicIds)
```

But runtime evaluation compares the `AllowedUsers` values against `IOperationContext.UserId`, which is the internal `UserProfile.Id`, not the feature flag record `PublicId` and not necessarily a user-facing public identifier.

Practical result:

- admins must currently supply the internal user profile GUID string that `IOperationContext.UserId` resolves to
- the label should be corrected or the comparison logic should change

### 3. Cache invalidation is not wired into CRUD

Feature changes may remain stale for up to 5 minutes after create, update, or delete.

### 4. Fine-grained permissions are defined but not enforced end-to-end

The permission model includes create/edit/delete keys, but the current page and service layer mostly rely on view-level access.

### 5. Partial rollout semantics may be surprising

When rollout is between `1` and `99`, the current implementation does not also require `IsEnabled = true`. The rollout bucket alone determines the result.

### 6. Anonymous or context-free evaluation is conservative

If evaluation depends on user context for rollout and the current request has no resolved user id, the result is `false`.

## Recommended Next Improvements

If you want this starter feature to be production-ready for new projects, the highest-value follow-ups are:

1. Add `OrganizationId` to the shared model and admin UI.
2. Invalidate feature-flag cache after create, update, and delete.
3. Decide whether partial rollout should require `IsEnabled = true`, then make the code explicit.
4. Fix the allowlist input semantics so the stored identifier matches the UI label.
5. Enforce `Create`, `Edit`, and `Delete` permission keys in both UI and service layer.
6. Add tests around evaluation precedence and rollout edge cases.

## File Reference Map

| Concern | File |
|---|---|
| Runtime evaluation | `src/Server/Admin/Services/FeatureFlags/FeatureFlagService.cs` |
| Admin CRUD | `src/Server/Admin/Services/FeatureFlags/FeatureFlagAdminService.cs` |
| Shared DTO | `src/Shared/Model/FeatureFlags/FeatureFlagItem.cs` |
| DB entity | `src/Server/Database/Core/Models/FeatureFlag.cs` |
| EF model config | `src/Server/Database/Core/ApplicationDbContext.cs` |
| Query implementation | `src/Server/Database/Core/Data/Queries/BasicsImplementation/CoreFeatureFlagQuery.cs` |
| Entity/DTO mapping | `src/Server/Database/Core/Data/Decorators/FeatureFlagDecorators.cs` |
| Admin page UI | `src/Server/Admin/WebService/UI/Pages/Admin/FeatureFlagsPage.razor` |
| Admin page logic | `src/Server/Admin/WebService/UI/Pages/Admin/FeatureFlagsPage.razor.cs` |
| Mock service | `mocks/Server/Admin/ServicesMocks/FeatureFlags/FeatureFlagAdminServiceMock.cs` |
| Permission constants | `src/Shared/Model/Permissions/PermissionDefinitions.cs` |

