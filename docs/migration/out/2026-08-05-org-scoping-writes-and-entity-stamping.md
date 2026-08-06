---
origin: DevCoreApp
targets: [ThreadIQ, Tentrie]
scope:
  - Server.Database.Core.Data.Queries.CoreDatabaseObjectQuery
  - Server.Database.Core.Data.OrganizationStampInterceptor
  - Server.Database.Core.ApplicationDbContext
  - Server.Admin.Services.Background.BackgroundRequestItem
  - Server.Admin.Services.Background.BackgroundWorker
  - Server.Admin.Services.Files.FileService
  - Server.Admin.Services.BaseService
  - Shared.Utils.DateTimeExtensions
status: pending
related:
  - docs/migration/in/2026-07-23-tentrie-soft-delete-and-unit-of-work.md
  - src/Server/Database/UnitOfWork.md
  - Diagnosed from ThreadIQ's fixes to the same surfaces (see 2026-08-05-threadiq-core-catchup.md)
---

# Fix write-side organization scoping and entity stamping under the per-operation unit of work

## Why

Adopting the per-operation unit of work (each service method opens its own short-lived context)
changed which context owns a given entity instance. Two classes of bug follow from that, and both
are silent in development and loud in production:

1. **Cross-context navigation assignment.** `AuthorizationContext.CurrentProfile` is materialized by
   the DI-scoped repository. Assigning it to a `CreatedBy`/`UpdatedBy` **navigation** on an entity
   owned by a *different*, short-lived context pulls that `UserProfile` into the new context's
   `Added` graph, so `SaveChanges` tries to `INSERT` the user and fails on a duplicate key.
2. **Unstamped `OrganizationId`.** The read-side organization filter is deliberately fail-open — an
   empty `VisibleOrganizationIds` disables filtering rather than returning nothing. A row that is
   written without an `OrganizationId` therefore reads back perfectly for an unscoped developer and
   disappears for every user with real organization claims.

Both live entirely in shared `Core` surfaces and affect every fork that has adopted the unit-of-work
change. ThreadIQ hit and diagnosed both independently; this doc carries the canonical fix.

## Current state

| Surface | Pattern today | Consequence |
|---|---|---|
| `Server.Admin.Services.Files.FileService` | `fileRecord.CreatedBy = AuthorizationContext.CurrentProfile` | Upload fails: EF tries to INSERT the current user |
| `Server.Database.Core.Data.Queries.CoreDatabaseObjectQuery` | `CreateNew()` sets `Id`/`PublicId`/dates only | Nothing populates `CreatedById`, so callers reach for the navigation |
| `Server.Admin.Services.Background.BackgroundWorker` | Builds the `BackgroundTask` row in a reset scope, never sets `OrganizationId` | Every job row is `Guid.Empty`; the Job Dashboard is empty for any scoped user |
| `Server.Database.Core.ApplicationDbContext` | `AuditInterceptor` only | No central guard — each new create path can silently skip scoping |

## Proposed change

### 1. Stamp creator/updater as scalar FKs, never navigations

In `CoreDatabaseObjectQuery`, stamp inside `CreateNew()` and `UpdateAsync()`. Guard on the runtime
type rather than narrowing the generic constraint — the class also serves entities that are only
`DatabaseObject`:

```csharp
private void StampCreatedBy(TEntity entity)
{
    if (entity is DatabaseEntityObject entityObject)
    {
        entityObject.CreatedById = CurrentProfile?.Id;
        entityObject.UpdatedById = CurrentProfile?.Id;
    }
}
```

`UpdateAsync` stamps `UpdatedById` only, and only when `CurrentProfile != null`, so a background
update does not blank an existing value.

Then remove every `X.CreatedBy = …` / `X.UpdatedBy = …` navigation assignment in services. In
DevCoreApp that was three lines in `FileService`; grep your fork for `CreatedBy = ` and
`UpdatedBy = ` excluding `ById`.

### 2. Central write-side organization stamping

Add `Server.Database.Core.Data.OrganizationStampInterceptor` — a `SaveChangesInterceptor` that, for
every `Added` entry implementing `IOrganizationScoped` whose `OrganizationId` is still `Guid.Empty`,
assigns `IOperationContext.PrimaryOrganizationId` and **throws** when none resolves:

```csharp
entry.Entity.OrganizationId = _operationContext.PrimaryOrganizationId
    ?? throw new InvalidOperationException(
        $"Cannot insert {entry.Metadata.ClrType.Name}: the row is organization-scoped but " +
        "the operation context resolves no PrimaryOrganizationId. …");
```

Register it in `ApplicationDbContext.OnConfiguring` alongside `AuditInterceptor`, so it covers both
the DI-scoped context and the short-lived ones from `IAppDbContextFactory`:

```csharp
optionsBuilder.AddInterceptors(
    new AuditInterceptor(_operationContext),
    new OrganizationStampInterceptor(_operationContext));
```

Use `entry.Metadata.ClrType.Name` in the message, not `GetTableName()` — the latter is a
relational-only extension and returns nothing under the in-memory provider used by tests.

An already-set `OrganizationId` is left alone, so a caller can still deliberately write into another
organization.

`BackgroundTask` is exempt via an allowlist (`UnscopedInsertsAllowed`). Its rows are built in a reset
scope with no ambient organization, and guarding them would fail unauthenticated confirmation and
password-reset mail outright rather than merely hide a job row.

### 3. Carry the submitter's organization onto background tasks

Add `Guid? OrganizationId` to `BackgroundRequestItem`. In `BackgroundWorker.SubmitAsync`, seed the
background operation context from it (after `Reset()`) and assign the row explicitly:

```csharp
if (item.OrganizationId.HasValue && item.OrganizationId.Value != Guid.Empty)
{
    operationContext.PrimaryOrganizationId = item.OrganizationId.Value;
    operationContext.SetVisibleOrganizationIds(new[] { item.OrganizationId.Value });
}
…
task.OrganizationId = item.OrganizationId ?? Guid.Empty;
```

Then populate it at every submit site that has an organization, injecting `IOperationContext` where
the service lacks it. In DevCoreApp that was `EmailLogService` (×2), `NotificationService`,
`UserProfileService`, `WebhookDispatcher`, and `ImportExportService` (which already had the context).

`IdentityEmailSender` is deliberately left unset: it is a **singleton** serving unauthenticated
Identity flows, so it cannot take a scoped `IOperationContext` and has no organization to record.
Leave a comment there rather than silently omitting it.

### 4. Timezone helpers

Add `NowInZone(tz)`, `ToLocal(tz)`, and `ToUtc(tz)` (with nullable overloads) to
`Shared.Utils.DateTimeExtensions`, and extract `BaseService.UserTimeZone` into a reusable
`protected static TimeZoneInfo? ResolveTimeZone(string? timeZoneId)` that returns null for an unset
or unrecognized id instead of throwing.

`NowInZone` matters under Blazor SSR specifically: `DateTime.Now` there is the *server's* clock —
UTC in most cloud deployments — not the user's.

## Affected shared surface

- `Server.Database.Core.Data.Queries.CoreDatabaseObjectQuery` — `CreateNew()`, `UpdateAsync()`
- `Server.Database.Core.Data.OrganizationStampInterceptor` — **new file**
- `Server.Database.Core.ApplicationDbContext` — `OnConfiguring` only
- `Server.Admin.Services.Background.BackgroundRequestItem` — new property
- `Server.Admin.Services.Background.BackgroundWorker` — `SubmitAsync`
- `Server.Admin.Services.Files.FileService` — remove navigation assignments
- `Server.Admin.Services.BaseService` — `UserTimeZone` → `ResolveTimeZone`
- `Shared.Utils.DateTimeExtensions` — additive
- Submit sites in `Server.Admin.Services.{Email,Notifications,UserAdmin,Webhooks,ImportExport}` —
  constructor + call-site changes, likely to differ per fork

**Collision risk.** A fork with product-specific background request types will have its own
`MapRequestType` / `ExtractResultReference` arms in `BackgroundWorker`; keep them. If your fork
already reads an organization off request payloads (ThreadIQ does), reconcile rather than replace —
`BackgroundRequestItem.OrganizationId` is meant to be the general path, with payload extraction as a
fallback for types that carry one.

## Verification

```bash
dotnet build DevInstance.DevCoreApp.slnx      # substitute your solution file
dotnet test  DevInstance.DevCoreApp.slnx
```

Add `OrganizationStampInterceptorTests` next to the existing organization query-filter tests, with
four cases (DevCoreApp's version is a working reference):

1. insert without an organization → stamped from `IOperationContext`
2. insert with an explicit organization → left alone
3. insert with no resolvable organization → `InvalidOperationException` naming the entity type
4. `BackgroundTask` inserts unscoped without throwing

Manual checks, both of which fail before the change:

- Upload a file as a user with organization claims — it should succeed rather than throw
  "An error occurred while saving the entity changes", and the `FileRecords` row should carry a
  non-empty `CreatedById` and `OrganizationId`.
- Queue any email as a signed-in user, then open the Job Dashboard — the row should be visible.

## Open questions

- Rows written before this change keep `OrganizationId = Guid.Empty` and stay invisible. DevCoreApp
  has not written a backfill; if your fork has meaningful history in `BackgroundTasks`,
  `FileRecords`, `ImportSessions`, or `Notifications`, decide on a one-off UPDATE to the tenant's
  root organization.
- The allowlist is currently just `BackgroundTask`. The real fix — threading the submitting user's
  organization through every request type — is what item 3 starts; once every submit site supplies
  one, the allowlist can be removed and the guard made absolute.
