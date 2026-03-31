# Settings

## Overview

The settings feature is a database-backed application configuration system for values that should be changeable at runtime without a redeploy. In the current starter implementation, it supports:

- system-scoped settings in the admin UI
- organization-scoped settings in the admin UI
- user-scoped settings in runtime code
- in-memory cached reads
- immediate cache invalidation on writes
- masking of sensitive values in admin list views
- startup seeding of settings already used by runtime services

This is not a mirror of `appsettings.json`. It is a separate database-backed configuration layer.

## Why This Feature Exists

This starter includes a settings system so application behavior can be adjusted at runtime and optionally vary by scope.

Typical use cases include:

- storage upload limits and policies
- appearance preferences
- tenant or organization branding
- future feature/module configuration that should not require a deployment

## What It Is Used For Today

The settings system is already used by runtime services.

### File service

`FileService` reads settings in category `Storage` for:

- `AllowedContentTypes`
- `MaxFileSizeBytes`
- `SoftDelete`

These affect upload validation and deletion behavior.

### Theme service

`ThemeService` reads and writes:

- `Appearance.Theme`

This is the current user’s theme preference, resolved through the settings service.

## High-Level Design

The settings feature is split across the persisted data model, runtime resolution service, admin CRUD service, and admin UI.

| Area | Responsibility | Main files |
|---|---|---|
| Data model | Stores scoped settings rows | `src/Server/Database/Core/Models/Setting.cs` |
| Query layer | Filters and searches setting records | `src/Server/Database/Core/Data/Queries/BasicsImplementation/CoreSettingsQuery.cs` |
| Decorators | Maps DB rows to admin DTOs | `src/Server/Database/Core/Data/Decorators/SettingDecorators.cs` |
| Runtime service | Resolves effective values for application code | `src/Server/Admin/Services/Settings/SettingsService.cs` |
| Cache invalidation | Invalidates cached runtime lookups after writes | `src/Server/Admin/Services/Settings/ISettingsCacheInvalidator.cs` |
| Admin CRUD service | Lists and mutates DB setting rows | `src/Server/Admin/Services/Settings/SettingsAdminService.cs` |
| Admin UI | Displays and edits settings | `src/Server/Admin/WebService/UI/Pages/Admin/SettingsPage.razor` |
| Seeder | Creates default settings already used by runtime features | `src/Server/Admin/Services/Seeding/SettingsDataSeeder.cs` |

## Data Model

The persisted record is `Setting`.

```csharp
public class Setting : DatabaseBaseObject
{
    public Guid? TenantId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? UserId { get; set; }

    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string ValueType { get; set; } = "string";
    public string? Description { get; set; }
    public bool IsSensitive { get; set; }
}
```

### Field meanings

| Field | Meaning |
|---|---|
| `Category` | Logical grouping such as `Storage` or `Appearance` |
| `Key` | Specific setting name inside the category |
| `Value` | Stored raw/JSON value |
| `ValueType` | Intended type hint such as `string`, `int`, `bool`, or `json` |
| `IsSensitive` | Whether admin views should mask the value |
| `TenantId` / `OrganizationId` / `UserId` | Scope markers |

### Scope model in the schema

The schema allows:

- system scope
- tenant scope
- organization scope
- user scope

But the current implementation does not expose all of those equally. The schema is broader than the admin surface.

## Current Effective Scope Behavior

### Runtime resolution

The runtime service currently resolves settings in this effective order:

1. user
2. organization
3. system

Tenant rows exist in the data model, but runtime tenant context resolution is not currently wired up.

### Admin UI

The admin settings page currently exposes only:

- `System`
- `Organization`

The tenant tab was removed because it was present in the UI before the actual tenant-resolution behavior was implemented.

### User scope

User-scoped settings are still supported by runtime code through `SettingsService.SetAsync` and `GetAsync`, which is how theme preference works today.

They are just not managed from the admin settings page.

## Runtime Resolution Service

`SettingsService` is the main runtime API.

### Main methods

| Method | Purpose |
|---|---|
| `GetAsync<T>(category, key)` | Resolve one setting for the current runtime context |
| `SetAsync<T>(category, key, value)` | Write at the most specific available runtime scope |
| `SetAsync<T>(..., tenantId, organizationId, userId)` | Explicit scoped write |
| `GetAllForCategoryAsync(category)` | Resolve all keys in one category for the current context |

### Context source

The service resolves context from:

- the current authenticated user
- `IOperationContext.PrimaryOrganizationId`

Tenant context is currently left `null`.

## Caching

Runtime reads are cached in memory for 10 minutes.

### Cache behavior

- cache key includes category, key, and resolved scope context
- cache invalidation uses a versioned key prefix
- writes increment the cache version for that category/key

This means admin writes now become visible immediately to runtime readers without needing to enumerate all possible scope-specific cache entries.

## Admin Settings Page

The admin page is:

```text
/admin/settings
```

### Permissions

The page now requires:

- `Admin.Settings.View`

Mutating actions additionally require:

- `Admin.Settings.Edit`

That means read-only settings access is now possible without automatically granting edit/delete capability.

### Current capabilities

The page supports:

- switching between system and organization scopes
- searching by category, key, or description
- creating settings
- editing setting values
- deleting settings
- masking sensitive values in the list

### Sensitive value behavior

Sensitive values are masked in list views. The admin page does not preload the raw secret into the visible list response.

An important fix was made here: saving a sensitive setting while its value is masked no longer overwrites the stored value with an empty string.

## Seeded Default Settings

`SettingsDataSeeder` now seeds settings that are already consumed by runtime services so the page is not empty on a fresh database.

### Current seeded rows

| Category | Key | Value | Type | Used by |
|---|---|---|---|---|
| `Storage` | `AllowedContentTypes` | `*` | `string` | `FileService` |
| `Storage` | `MaxFileSizeBytes` | `10485760` | `int` | `FileService` |
| `Storage` | `SoftDelete` | `false` | `bool` | `FileService` |
| `Appearance` | `Theme` | `System` | `string` | `ThemeService` |

### Why these are seeded

Before this seeder existed, the settings page could be empty even though runtime services were already reading from the settings system and falling back to hardcoded defaults. Seeding makes the feature visible and editable immediately.

## Difference From `appsettings.json`

This settings system is separate from application configuration in `appsettings.json`.

### `appsettings.json` is for

- app startup wiring
- infrastructure configuration
- provider registration
- deployment/environment configuration

Examples:

- database provider
- connection strings
- SMTP provider registration values
- background task settings
- health endpoint protection settings

### database settings are for

- runtime-adjustable business or feature behavior
- scoped overrides
- values that an admin may reasonably change after deployment

Examples:

- file upload policy
- per-user theme preference
- future org-scoped branding or module toggles

## Value Storage and Types

The service supports these common type hints:

- `string`
- `int`
- `bool`
- `json`

The stored `Value` is still essentially a serialized string payload, and deserialization depends on the requested generic type in `GetAsync<T>`.

For simple cases:

- strings are unwrapped
- ints are parsed
- bools are parsed
- other types fall back to JSON deserialization

## Current Limitations

The most important current limits are:

### 1. Tenant scope is reserved, not fully active

The schema supports tenant-scoped records, but the current runtime context does not resolve a tenant id and the admin page does not expose tenant settings.

### 2. Sensitive values are masked, not fully secret-managed

The masking behavior is appropriate for the admin list view, but this is not a full secrets-management system with rotation, audit-specific redaction workflows, or secure reveal flows.

### 3. User scope exists only through runtime code

The admin page does not manage user-scoped rows directly.

### 4. It is still an application-level cache

The cache invalidation fix handles this process correctly, but if you later run multiple app instances and need cross-node cache coherence, this will need to evolve beyond simple in-memory cache versioning.

## Recommended Usage

Use the settings system for:

- business or feature values that should change without redeploying
- values that may vary by organization or user
- values where a database-backed admin workflow makes sense

Do not use it as the first choice for:

- low-level infrastructure wiring
- secrets that need a real secret manager
- large structured configuration blobs unless you truly need them

## Summary

The settings feature is a real runtime configuration layer, not just an admin stub. Today it is primarily useful for:

- system-scoped application settings
- organization-scoped overrides
- user-scoped preferences managed by runtime services

After the recent fixes, it is in a much better state:

- sensitive values are safer to edit
- runtime cache invalidation is immediate
- the admin permission boundary is clearer
- the page now shows seeded settings that are actually used by the application

The main caveat is that tenant support remains schema-level groundwork rather than a finished end-to-end feature.
