---
origin: DevCoreApp
targets: [ThreadIQ]
scope:
  - Server.Api.Core.Identity.ApiKeyAuthenticationHandler
  - Server.Api.Program                                   # AddDatabase runtime provider switch (root file)
  - Server.Api.Core.Health.StuckEmailsHealthCheck
  - Server.EmailProcessor.Smtp.Core.SmtpEmailProvider
  - Server.Database.SqlServer.ConfigurationExtensions    # per-operation unit-of-work factory
  - Server.Database.Core.Data.Queries.BasicsImplementation.CoreQueryRepository   # IDisposable
  - Server.Database.Core.Data.Decorators.*               # timezone parameters removed (UTC on the wire)
  - Server.Services.Core.AccountService
  - Server.Services.Core.Authentication.JwtAuthService
  - Server.Services.Core.Authentication.OrganizationContextResolver
  - Server.Services.Core.Authentication.PermissionClaimsTransformation
  - Server.Services.Core.ApiKeys.ApiKeyPermissionSnapshotService
  - Server.Services.Core.Roles.RoleManagementService
  - Server.Services.Core.Seeding.ApiKeyDataSeeder
  - Server.Services.Core.Settings.SettingsService / SettingsAdminService
  - Server.Services.Core.FeatureFlags / Webhooks / ApiKeys admin services   # org scoping
  - Server.Services.Core.Email (SendEmailTaskHandler, EmailSenderService)
  - Client.Desktop.Core.UI.Pages.Admin.SettingsPage
  - Client.Mobile UI/App.razor
status: pending
related:
  - docs/migration/out/2026-11-07-core-fixes-from-threadiq-sync.md   # apply first
---

# ThreadIQ catch-up: shared Core where DevCoreApp is ahead

## Delivery & audit log

| Date | Event |
|---|---|
| 2026-11-07 | Authored in DevCoreApp `out/` from the 2026-11-07 shared-surface diff (ThreadIQ `main` `13310e5`). Not yet delivered. |

## Why

The 2026-11-07 file-by-file diff showed shared Core that DevCoreApp has changed and ThreadIQ has
not. Most of it came in through the DevCoreApp-side work on the WASM move (#1179) and the
post-restructure fixes (#1133/#1134), after ThreadIQ took its copy. Under the sync-key rule these
files are the same shared surface, so ThreadIQ should converge on the canonical versions. Apply
`2026-11-07-core-fixes-from-threadiq-sync.md` first; it touches some of the same files.

## Current state → change

**Data access.** In these, ThreadIQ still uses `ApplicationDbContext` directly, which breaks the
"services never call DbContext" rule; DevCoreApp goes through query classes:

| Surface | DevCoreApp (canonical) |
|---|---|
| `AccountService`, `JwtAuthService`, `OrganizationContextResolver`, `PermissionClaimsTransformation`, `RoleManagementService`, `ApiKeyPermissionSnapshotService`, `ApiKeyDataSeeder` | Query-layer calls (`GetRefreshTokenQuery`, `GetRolePermissionQuery`, `GetUserOrganizationQuery`, `GetUserPermissionOverrideQuery`, …). The supporting query interfaces and implementations exist in DevCoreApp's `Database/Core`; port them with the services. |
| `StuckEmailsHealthCheck` | Uses `CountStuckQueuedAsync` on the query instead of the DbContext. |
| `ApiKeyAuthenticationHandler` | Loads the key through `IApiKeyQuery`. **It also rejects a key whose owner's account is not `AccountStatus.Active`**; ThreadIQ has no such check, so a disabled user's keys keep working. Treat this as a security fix. |

**Infrastructure:**

| Surface | DevCoreApp (canonical) |
|---|---|
| `Program.AddDatabase` | Picks Postgres or SqlServer at runtime from `Database:Provider`. |
| SqlServer `ConfigurationExtensions` | Registers the per-operation unit-of-work factory (`IAppDbContextFactory` / `IQueryRepositoryFactory`). ThreadIQ's SqlServer path has none, so services that use `RepositoryFactory` cannot run on SQL Server. |
| `CoreQueryRepository` | Also implements `IDisposable`, so a synchronous scope disposal does not throw. There's a test in `tests/Server/Database/Core/UnitOfWorkConcurrencyTests`. |
| Decorators | Timezone parameters removed. Every `DateTime` stays UTC on the wire, and the client converts it (`ILocalTimeService`). |
| `SmtpEmailProvider` | Implemented; ThreadIQ's is a stub. |

**Behavior:**

| Surface | DevCoreApp (canonical) |
|---|---|
| `SettingsService` / `SettingsAdminService` | Cache versioning and invalidation on write; scope guards in the admin service. |
| FeatureFlag / Webhook / ApiKey admin services + decorators | Organization resolution on create; reload after save; `OrganizationId` on the decorators. ThreadIQ fenced these as "not adopted". Either adopt them now or keep the fence and record why in your `in/` copy of this doc. |
| Webhook delivery | Failed deliveries are marked failed. |
| `SendEmailTaskHandler` / `EmailSenderService` | Guards against duplicate sends and multi-recipient payloads. |
| Job dashboard | `CreatedById` mapping fix. |
| `ApiKeyDataSeeder` | Null guard. |
| Desktop `SettingsPage` | View/Edit split. |
| Mobile `UI/App.razor` | NotFound page and a no-permission message. |

## Affected shared surface

Everything in the front-matter. Watch for collisions with ThreadIQ fences:
- `PermissionClaimsTransformation` and the import-template fences are replaced by the companion doc.
- The org-scoping "not adopted" fences: see the behavior table above.
- `Program.cs`: keep the Mobile `/_content` rewrite fence.

## Verification

- `dotnet build` in `Debug`, `ServiceMocks` and `Release`; `dotnet test`.
- `grep -rl ApplicationDbContext src/Server/Services/Core` should list only `Authentication/ConfigurationExtensions.cs` and seeders that legitimately migrate or seed.
- Disable a user, then call the API with one of their keys → 401.
- With `Database:Provider = SqlServer`, the app starts and a page that writes data works.

## Open questions

- Per-operation repositories: DevCoreApp adopted ThreadIQ's #883 change (`2c6af628`) for
  `SettingsService`, `FeatureFlagService` and `WebhookDispatcher`, and extended it to
  `RoleManagementService`, `JwtAuthService` and `ApiKeyPermissionSnapshotService`. See item 10 of
  `2026-11-07-core-fixes-from-threadiq-sync.md`. ThreadIQ's copies of those three still use
  `ApplicationDbContext` directly (data-access table above); port the canonical versions, which
  use queries and the per-operation factory.
