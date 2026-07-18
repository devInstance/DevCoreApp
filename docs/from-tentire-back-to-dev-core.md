# Back to DevCoreApp — items to contribute upstream

Patterns and features developed in Tentrie that generalize beyond this product and belong in [DevCoreApp](https://github.com/devInstance/DevCoreApp) (the underlying starter template). Tracked here so we don't lose them when picking up upstream work.

---

## 1. Soft delete framework (replacing `IsActive`)

### Why

Once a record is referenced by financial / payroll / audit data it must be preservable. Physical deletes orphan references and break reconciliation. The current `IsActive` flag deactivates a row but captures nothing about *who* did it, *when*, or *why*, and there's no consistent way to ensure list queries hide it. A first-class soft-delete framework in DevCoreApp gives every product built on it uniform semantics, an audit trail, and a global EF query filter — without each product reinventing the pattern.

### Current state in Tentrie

| Surface | Pattern | Notes |
|---|---|---|
| `EmployeeItem`, `EquipmentItem` (and the spec'd `JobItem`) | `bool IsActive = true` | Just a column. No audit. No global filter — every query that wants to hide inactive must opt in. |
| `FileRecord` | Soft delete controlled by the `Storage:SoftDelete` setting | Implemented in `FileService.DeleteAsync`. One-off pattern, not reused elsewhere. |
| `DailyTicket` | Hard delete, restricted to `Draft` only | Server `DeleteAsync` throws `BusinessRuleException` for any non-Draft status. |
| Global `HasQueryFilter` in `ApplicationDbContext` | Organization-scope only | Not filtering on a deletion column. |
| `docs/Specification.md:911` | Aspirational mention of `IsDeleted` / `DeletedAt` | No entity implements it. |

### Proposed model (in DevCoreApp)

1. **`ISoftDeletable` interface** carrying the three audit fields:
   ```csharp
   public interface ISoftDeletable
   {
       bool IsDeleted { get; set; }
       DateTime? DeletedAt { get; set; }
       Guid? DeletedById { get; set; }     // FK to UserProfile
   }
   ```

2. **Base entity** — extend the existing `DatabaseEntityObject` (which already tracks `CreatedBy` / `UpdatedBy`) with the three columns when the entity opts into `ISoftDeletable`. EF configuration adds the navigation property `DeletedBy → UserProfile`.

3. **Global EF query filter**, composed with the existing org-scope filter:
   ```csharp
   builder.Entity<T>().HasQueryFilter(e =>
       !e.IsDeleted
       && (_operationContext.VisibleOrganizationIds == null
           || _operationContext.VisibleOrganizationIds.Count == 0
           || _operationContext.VisibleOrganizationIds.Contains(e.OrganizationId)));
   ```
   Standard `.IgnoreQueryFilters()` escape hatch for admin / reporting / archival queries.

4. **Service-layer base method** `SoftDeleteAsync(entity)` on `BaseService`:
   - Sets `IsDeleted = true`, `DeletedAt = TimeProvider.CurrentTime`, `DeletedById = AuthorizationContext.CurrentProfile.Id`.
   - Calls `query.UpdateAsync(entity)` — the EF interceptor still writes a normal audit-log row for the change.
   - Companion `RestoreAsync` (clears the three fields) and `HardDeleteAsync` (true `Remove`) for explicit admin operations.

5. **Audit consistency** — the existing `SaveChangesInterceptor` already logs property-level changes to `AuditLogs`; soft-delete just becomes another tracked change.

### Lifecycle rule (project-wide convention)

The rule we'll enforce across Tentrie (and should be the default expectation in DevCoreApp): **never physically delete anything past the `Draft` stage.**

- **Draft stage** → physical (hard) delete is OK. No downstream references exist. The foreman deleting a draft ticket truly removes it.
- **Post-Draft** (`Submitted` / `Approved` / `Rejected`) → soft delete only. Workflow verbs:
  - **Reject** — reviewer sends the ticket back with a required reason; status returns to `Draft` so the foreman can revise and resubmit. Already implemented.
  - **Discard** *(new)* — reviewer explicitly removes a ticket from downstream processing (duplicate, submitted in error, etc.). Sets `IsDeleted = true`. Audit row records who / when / why. Retains the full ticket data.
  - **Approve** — terminal. No removal possible after this point; payroll has likely already pulled it.

### `IsActive` is removed everywhere — there is no dual flag

**Decision:** `IsActive` is removed from every entity that currently carries it. There is **no** dual `IsActive` + `IsDeleted` model. The framework introduces exactly one deletion flag — `IsDeleted` — and that's the only thing the global query filter looks at.

Where today's `IsActive` was carrying a real *operational* meaning (employee no longer with the company, job closed, equipment decommissioned), that need is **handled separately, per entity, in a future change** — typically by an entity-specific status enum where the lifecycle is rich enough to justify one. Examples that already exist:

- `JobStatus` (`Active` / `Completed` / `OnHold`) — already captures the operational lifecycle for jobs.
- `EquipmentUnitStatus` (`Available` / `InUse` / `OutOfService`) — already captures the operational lifecycle for equipment.

For entities without an existing status (Employee, etc.), the operational concept will be modeled deliberately when there's a concrete requirement — **not** by reintroducing a generic `IsActive`. This document does not prescribe what those replacements look like; it just records that we're not solving operational deactivation here.

### Affected entities (initial scope)

| Entity | Today | After the change |
|---|---|---|
| `Employee` | `IsActive` (drop) | `IsDeleted` + audit columns |
| `Equipment` | `IsActive` (drop) | `IsDeleted` + audit columns. Operational state already covered by `EquipmentUnitStatus`. |
| `Job` | `IsActive` per spec (drop) | `IsDeleted` + audit columns. Operational state already covered by `JobStatus`. |
| `ReportingYard` | (none) | `IsDeleted` + audit columns |
| `UnionLocal` | (none) | `IsDeleted` + audit columns |
| `DailyTicket` | Hard delete (Draft only) | Hard delete (Draft only) **plus** `Discard` (post-Draft) → soft delete |
| `FileRecord` | Per-feature `Storage:SoftDelete` setting | Migrate to the standard `IsDeleted` pattern; retire the dedicated setting |

### Implications

- **API list endpoints** transparently exclude soft-deleted rows via the global filter; no per-query opt-in needed.
- **Reports / payroll exports** use `.IgnoreQueryFilters()` when archival data must appear.
- **Foreign key references** stay valid — referenced rows still exist, so historical tickets keep showing the employee / equipment / job they were tied to.
- **PWA cache** — for an active ticket, the client should keep showing crew / equipment names from a now-soft-deleted master record (frozen historical reference). Achieved either by including the soft-deleted rows in the ticket-detail response, or by relying on the names already denormalized into the ticket (which today's `CrewEntry.EmployeeName` / `EquipmentEntry.EquipmentDescription` already do).
- **Audit trail** — `DeletedAt` / `DeletedById` plus the existing audit-log captures the full who/when of every removal. Currently `IsActive` says none of that.
- **Migration from `IsActive`** — straightforward: rename column, add `DeletedAt` / `DeletedById` (nullable, backfill `DeletedAt = MigrationDate` and `DeletedById = NULL` for rows currently `IsActive = false`), then add the global filter. Code changes are localized to whatever currently reads `IsActive`.

### DevCoreApp template — explicit upstream change

If DevCoreApp's base entity or scaffolded templates still carry an `IsActive` column (or expose `IsActive` on the base entity / generated DTOs / generated CRUD), **replace it with `IsDeleted`** as part of introducing the soft-delete framework. Specifically:

- Remove `IsActive` from any base entity class, generated migration template, model item, decorator (`ToView` / `ToRecord`), and DTO.
- Replace it with the three soft-delete fields (`IsDeleted`, `DeletedAt`, `DeletedById`) on entities that opt into `ISoftDeletable`.
- Generated CRUD scaffolding should call `SoftDeleteAsync` (the new base service method), not set `IsActive = false`.
- Scaffolded list pages stop offering the "Show inactive" toggle and instead offer "Show deleted" (admin-only, uses `.IgnoreQueryFilters()`).
- Update the template docs (the equivalent of `docs/Specification.md:911` upstream) so the aspirational `IsDeleted` mention becomes the documented pattern.

If DevCoreApp already provides `IsDeleted` (no `IsActive`), Tentrie just consumes the framework and migrates its existing `IsActive` columns away.

### Open questions

- Should `Approve` ever be reversible? Spec today says no (approved tickets are immutable, payroll has them). Keep it that way unless a real workflow need emerges.
- Restrict `HardDeleteAsync` to a specific permission (`Owner` / `Admin` only) for GDPR-style "remove all traces" operations? Probably yes.

---

## 2. Per-operation `DbContext` (factory + unit of work) — fix shared-context concurrency

### Why

In Blazor interactive Server, a circuit is a single DI scope shared by **every component on the page**. A `DbContext` registered `Scoped` (the EF default) therefore becomes one instance shared across all components for the circuit's lifetime. `DbContext` permits only one operation in flight at a time, so as soon as two components issue overlapping async queries — even via interleaved `await` on the single render dispatcher, not true parallel threads — EF throws:

> *A second operation was started on this context instance before a previous operation completed.*

This is a structural defect of the "scoped context" default under Blazor, not a product bug. Every app built on DevCoreApp inherits it. The canonical fix (Microsoft's own guidance) is to stop sharing one context across the circuit and instead create a **short-lived context per logical operation** via `IDbContextFactory<T>`.

### Current state in Tentrie

| Surface | Pattern | Consequence |
|---|---|---|
| `ConfigurePostgresDatabase` / `ConfigureSqlServerDatabase` | `AddDbContext<ApplicationDbContext, {Provider}ApplicationDbContext>(...)` → **Scoped** | One context per circuit. |
| `IQueryRepository` (`CoreQueryRepository`) | **Scoped**, holds one injected `ApplicationDbContext`; every `Get{Entity}Query()` hands that **same** instance to the query class | All queries in a circuit share one context → the collision above. |
| Identity (`AddEntityFrameworkStores<T>`) | Resolves the same scoped context | UserManager/SignInManager share it too. |
| `ApplicationDbContext` ctor | `(DbContextOptions, IOperationContext)`; `IOperationContext` is used in `OnConfiguring` (audit interceptor) and `OnModelCreating` (org query filters) | The context is inseparable from the request-scoped `IOperationContext` — rules out pooling. |

### Complications this pattern must solve (and DevCoreApp should solve once, generically)

1. **Abstract context + multiple providers.** `ApplicationDbContext` is abstract; each provider registers a concrete subtype. The built-in `AddDbContextFactory<TContext>` wants a concrete, instantiable type and only flows `DbContextOptions` — it won't inject `IOperationContext`. **Introduce a provider-agnostic factory abstraction in the Core/Database project:**
   ```csharp
   // Core (provider-agnostic) — consumed by the query layer
   public interface IAppDbContextFactory { ApplicationDbContext CreateDbContext(); }
   ```
   Implemented per provider, constructing the concrete subtype with options + the **scoped** `IOperationContext` (mirrors the existing `DesignTimeDbContextFactory`, which already constructs `new {Provider}ApplicationDbContext(options, operationContext)`):
   ```csharp
   public sealed class PostgresAppDbContextFactory : IAppDbContextFactory
   {
       private readonly DbContextOptions<PostgresApplicationDbContext> _options;
       private readonly IOperationContext _operationContext;
       public PostgresAppDbContextFactory(DbContextOptions<PostgresApplicationDbContext> options, IOperationContext operationContext)
       { _options = options; _operationContext = operationContext; }
       public ApplicationDbContext CreateDbContext() => new PostgresApplicationDbContext(_options, _operationContext);
   }
   ```
   Register options as **singleton**, the factory as **scoped** (so each created context binds to the current scope's `IOperationContext`, keeping audit + org filters correct).

2. **Unit-of-work boundary.** The architecture deliberately allows *atomic multi-entity writes inside a single query method* and lets a service coordinate several queries. That requires those queries to share **one** context for the duration of **one** operation — but not for the whole circuit. So the context's lifetime must equal one *logical operation*, not one query and not one circuit. Model the repository itself as the unit of work:
   ```csharp
   public interface IQueryRepository : IAsyncDisposable { /* Get{Entity}Query(...) as today */ }
   public interface IQueryRepositoryFactory { IQueryRepository Create(); }   // creates a repo over a fresh context
   ```
   Services take `IQueryRepositoryFactory` and open one unit of work per method:
   ```csharp
   await using var repo = _repoFactory.Create();
   var ticket = await repo.GetDailyTicketQuery(profile).ByPublicId(id).Select().FirstOrDefaultAsync();
   ```
   Disposing the repo disposes its context deterministically. Concurrent components now each open their own unit of work → no shared context → no collision.

3. **Identity & startup utilities still need a scoped context.** `AddEntityFrameworkStores` and one-shot seeders/migration/reconciliation utilities resolve `ApplicationDbContext` directly and can't use the factory. **Keep the existing scoped `AddDbContext` registration** alongside the new factory; the two coexist. (Identity calls rarely interleave with themselves, so the residual risk is acceptable; document it.)

4. **No pooling.** `AddPooledDbContextFactory` is incompatible because the context constructor carries scoped `IOperationContext` state. Use a non-pooled, scoped factory.

### Implications

- Audit `SaveChangesInterceptor` and the org-scope global query filters keep working unchanged, because the scoped factory injects the live `IOperationContext` at construction time.
- Background workers already create a DI scope per job; they should obtain their context/repository through the same factory rather than resolving `ApplicationDbContext` from the scope (see §3).
- This is a *lifetime* change, not a query-rewrite: individual query-method bodies don't change; what changes is who owns the context and for how long.

### DevCoreApp template — explicit upstream change

- Ship `IAppDbContextFactory` + provider implementations and the `IQueryRepositoryFactory` / unit-of-work shape as the **default** data-access wiring, so apps don't inherit the shared-scoped-context defect.
- Scaffolded services should be generated against `IQueryRepositoryFactory` with the `await using var repo = ...` idiom, never a long-lived injected repository or context.
- Keep a scoped `DbContext` registration in the template solely for Identity + seeders, with a comment explaining why both exist.

### As built in Tentrie (reference implementation to port upstream)

Shipped — the exact shape to lift into DevCoreApp:

- **`IAppDbContextFactory`** (Core, `Database/Core/Data/`) — `ApplicationDbContext CreateDbContext()`. Provider impls: `PostgresAppDbContextFactory` (public) and `SqlServerAppDbContextFactory` (`internal`, because `SqlServerApplicationDbContext` is internal — a public factory ctor can't take an internal type). Each holds a **built-once** `DbContextOptions<TProviderContext>` plus the scoped `IOperationContext` and `new`s the concrete context per call.
- **Registration** (each provider's `ConfigureXxxDatabase`): keep `AddDbContext<ApplicationDbContext, TProviderContext>(...)` (Identity + seeders + any code that opens its own DI scope) **and** add, alongside it:
  ```csharp
  var unitOfWorkOptions = new DbContextOptionsBuilder<TProviderContext>().UseXxx(conn, ...).Options; // built once
  services.AddScoped<IAppDbContextFactory>(sp =>
      new XxxAppDbContextFactory(unitOfWorkOptions, sp.GetRequiredService<IOperationContext>()));
  services.AddScoped<IQueryRepositoryFactory, QueryRepositoryFactory>();
  ```
  Passing the options via the registration closure (not a global `DbContextOptions<T>` DI registration) avoids clobbering `AddDbContext`'s own options. Factory is **scoped**, **non-pooled**.
- **`IQueryRepository : IAsyncDisposable`.** `CoreQueryRepository` is now concrete with an `ownsContext` flag: the DI-scoped registration constructs it `ownsContext: false` (DI owns the context; `DisposeAsync` is a no-op); the factory constructs it `ownsContext: true` (disposes the context it created).
- **`IQueryRepositoryFactory` / `QueryRepositoryFactory`** (Core) — `Create()` pulls a context from `IAppDbContextFactory` and returns `new CoreQueryRepository(..., ownsContext: true)`.
- **Who uses which:** Blazor-circuit services (every `BaseService` subclass + circuit-injected services like settings/roles/JWT/claims-transformation/org-context) take `IQueryRepositoryFactory` and open `await using var repo = ...` per method. Code that already opens its own DI scope per unit of work (background workers, import handlers, `HttpOperationContext`, reconciliation/smoke utilities) keeps resolving the **scoped** `IQueryRepository` — it's already isolated.
- **`BaseService`** exposes `RepositoryFactory` (the `Repository` property is gone). Per-method idiom: `await using var repo = RepositoryFactory.Create();` then `repo.GetXxxQuery(...)`.
- **Unit-of-work boundary rule** that keeps it correct: within one method, an entity that is **loaded and then mutated+saved via change tracking** (e.g. a load → `SaveChangesAsync()` aggregate, or a graph removed via `RemoveRangeAsync` on tracked children) must use the *same* method-local `repo`. Read-only resolve helpers may each open their own `repo`. Where a graph-loading helper feeds a mutating caller (Tentrie: `DailyTicketWorkflowService.LoadWithStagesAsync`), thread the caller's `repo` into the helper rather than letting it open its own. (Entities saved via a full `query.UpdateAsync(entity)` / `AddAsync` tolerate a detached entity from another repo, so simple CRUD helpers don't need threading — only tracking-based saves do.)
- **Tests:** a tiny non-owning `TestQueryRepositoryFactory(IQueryRepository)` whose `Create()` returns one shared repo over the in-memory test context lets existing tests keep a single context. A concurrency regression test (`UnitOfWorkConcurrencyTests`) fans out overlapping operations through the real factory and asserts they don't collide.

### Open questions

- Decided for Tentrie: the **explicit `await using var repo = factory.Create()`** form (not a delegate wrapper) — transparent in diffs and the natural C# idiom. Keep this as the scaffolding default upstream.
- Worth adding an analyzer/architecture test that flags a `Scoped` `DbContext` being injected outside the Identity/seeder allow-list (locks in both this and §3).

---

## 3. Services must never inject `DbContext` — route all data access through query classes

### Why

The layering rule ("only query classes touch `DbContext`/LINQ/`SaveChanges`") is what makes §2 possible: if data access is already funnelled through `IQueryRepository`, switching the context to a per-operation lifetime is a localized change. Where services hold their own `ApplicationDbContext`, the factory change can't reach them and the concurrency defect persists. This cleanup is both a correctness prerequisite for §2 and a standalone architecture-hygiene win, so it should be done **first**.

### Current state in Tentrie

~16 runtime services/handlers inject `ApplicationDbContext` (or resolve it from a scope) and run LINQ / `SaveChanges` / `ExecuteUpdateAsync` directly — e.g. `SettingsService`, `DailyTicketService`, `JobService`, `UserProfileService`, `RoleManagementService`, `PermissionClaimsTransformation`, `JwtAuthService`, `OrganizationContextResolver`, `ApiKeyPermissionSnapshotService`, `TicketValidationService`, `BackgroundTaskWorker`, `ApiKeyAuthenticationHandler`. (Seeders, migration pre-checks, reconciliation, and smoke-test utilities legitimately use the context at startup and are exempt.)

### Proposed model

- Each violating service depends only on `IQueryRepository` (+ domain services), never on `ApplicationDbContext`.
- Every direct LINQ/`SaveChanges`/`ExecuteUpdateAsync` site moves into a method on the relevant `I{Entity}Query` (return materialized data, never a raw `IQueryable`).
- Cross-entity / Identity-joining reads that have no single owner get a feature-specific query (the codebase already does this with `IWorkflowOwnerQuery`) registered on `IQueryRepository` — e.g. a permissions/claims query for `PermissionClaimsTransformation`, a refresh-token query for `JwtAuthService`, a settings-resolution query for `SettingsService`.
- Bulk `ExecuteUpdateAsync` (e.g. `BackgroundTaskWorker`'s claim/recover sweeps) becomes a bulk method on the query.

### DevCoreApp template — explicit upstream change

- Generated service templates must not take a `DbContext` constructor parameter; scaffolding emits `IQueryRepository` usage.
- Add an **architecture test** (NetArchTest / Roslyn analyzer) to the template that fails the build if any type outside the query namespace (and the seeder/Identity allow-list) references `DbContext`, `DbSet<>`, or `Microsoft.EntityFrameworkCore` query operators. This is the enforcement that keeps §2 viable over time.

### Open questions

- Where should genuinely cross-cutting auth reads live (claims transformation, org-context resolution)? Likely a dedicated `IAuthorizationQuery` / `IOrganizationContextQuery` on the repository rather than scattering them across entity queries.
