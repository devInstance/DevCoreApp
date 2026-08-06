# CLAUDE.md — DevCoreApp Solution Guide

This is the root-level guide for the entire DevCoreApp solution. Project-specific conventions (page patterns, service patterns, mocks) are in `src/Server/Admin/WebService/CLAUDE.md`.

## What Is This Project?

DevCoreApp is a reusable starter template for custom ERP and CRM applications. It provides user management, permissions, organization hierarchy, background jobs, email, notifications, and file storage out of the box. Each new project forks this template and builds domain features on top.

## Tech Stack

- .NET 10+, ASP.NET Core, Entity Framework Core, ASP.NET Identity
- Blazor **interactive Server** (Admin UI), Blazor WebAssembly (field worker client — standalone, not yet hosted)
- PostgreSQL (primary), SQL Server (secondary)
- DevInstance.BlazorToolkit — client-side Blazor utilities (`[BlazorService]`, `IApiContext<T>`, `IServiceExecutionHost`)
- DevInstance.WebServiceToolkit — server-side utilities (`[WebService]`, `[QueryModel]`, `ModelItem`, `ModelList<T>`, `HandleWebRequestAsync()`, `IModelQuery<T,D>`)
- DevInstance.LogScope — scope-based logging (`IScopeManager`, `IScopeLog`)

## Build, Test, and Run Commands

The solution file is `DevInstance.DevCoreApp.slnx` (the modern `.slnx` XML format — there is no `.sln`). All commands run from the repo root.

```bash
# Restore + build the whole solution
dotnet build DevInstance.DevCoreApp.slnx

# Run the Admin web app (Blazor SSR host + API). Needs a reachable database (see below).
dotnet run --project src/Server/Admin/WebService/DevCoreApp.Admin.WebService.csproj

# Run the app against in-memory mock services — no database/Identity/email needed.
# The SERVICEMOCKS preprocessor symbol swaps real services for [BlazorServiceMock] ones.
dotnet run -c ServiceMocks --project src/Server/Admin/WebService/DevCoreApp.Admin.WebService.csproj

# Run all tests (xUnit v3)
dotnet test DevInstance.DevCoreApp.slnx

# Run one test project
dotnet test tests/Server/WebService/WebService.Tests.csproj

# Run a single test (or class) by name filter
dotnet test tests/Server/WebService/WebService.Tests.csproj --filter "FullyQualifiedName~MyTestClass.MyTestMethod"
```

- Three build configurations exist: `Debug`, `Release`, and `ServiceMocks`. CI (`azure-pipelines-ci.yml`) builds `Release` and runs `**/tests/**/*[Tt]ests.csproj`.
- Two test projects, both xUnit v3: `tests/Server/Database/Core/Core.Tests.csproj` (org query
  filter, unit-of-work concurrency) and `tests/Server/WebService/WebService.Tests.csproj` (service
  tests). Shared fakes (`IScopeManagerMock`, `TimerProviderMock`) live in `tests/Shared/TestUtils`.
- **Database provider is selected at runtime** via `Database:Provider` in `appsettings.json` (`Postgres` — default — or `SqlServer`), with connection strings under `ConnectionStrings:PostgresConnection` / `:SqlServerConnection`. The `Database/` solution folder splits into `Core` (provider-agnostic) plus `Postgres` and `SqlServer` projects. **Each provider owns its own `Migrations/` folder** — a schema change needs a migration in both.
- On startup the app calls `MigrateAndSeedAsync()`: it applies pending migrations and runs every
  registered `IDataSeeder` (organizations, settings, permissions, API keys). Adding seed data means
  adding an `IDataSeeder`, not a migration insert.
- After changing entities, **do not** scaffold EF migrations yourself — see the rule in "Things To Never Do".

## Solution Structure

Folder names on disk are short; **assembly/namespace names are long**. The root namespace for
everything is `DevInstance.DevCoreApp.{Server|Shared|Client}...` — that prefix is what forks
rename, and what the migration "sync key" strips (see [Cross-Project Sync](#cross-project-sync-migration-inout)).

```
DevCoreApp/
├── src/
│   ├── Client/
│   │   ├── DevCoreApp.Client/       # Blazor WASM app (standalone — NOT hosted by WebService today)
│   │   └── Client.Services/         # → DevInstance.DevCoreApp.Client.Services (API clients)
│   ├── Server/
│   │   ├── Admin/
│   │   │   ├── Services/            # → DevCoreApp.Admin.Services  (business logic, auth, background)
│   │   │   └── WebService/          # → DevCoreApp.Admin.WebService (Blazor SSR host + API + SignalR)
│   │   ├── Database/
│   │   │   ├── Core/                # Provider-agnostic: entities, queries, decorators, interceptors
│   │   │   ├── Postgres/            # PostgresApplicationDbContext + its own Migrations/
│   │   │   └── SqlServer/           # SqlServerApplicationDbContext + its own Migrations/
│   │   ├── Email/                   # Processor (abstraction) + MailKit / SendGrid / Smtp providers
│   │   └── Storage/                 # Processor (abstraction) + Local / S3 providers
│   └── Shared/
│       ├── Model/                   # ViewModels/DTOs by feature → DevCoreApp.Shared.Model.<Feature>
│       └── Utils/                   # Shared helpers (ITimeProvider, etc.)
├── mocks/Server/Admin/ServicesMocks/ # [BlazorServiceMock] services for UI development
├── tests/                            # Mirrors src/ (xUnit v3 — see Build, Test, and Run Commands)
└── docs/
```

**There is no separate Worker process.** Background jobs run in-process inside WebService as a
hosted service — see [Background Jobs](#background-jobs).

## Dependency Rules — Do Not Violate

```
Shared.Model / Shared.Utils    ← Referenced by everything. No project dependencies.
Database.Core                  ← References: Shared
Database.Postgres/.SqlServer   ← References: Database.Core
Email.* / Storage.*            ← References: Shared    (Processor = abstraction, rest = providers)
Admin.Services                 ← References: Database.Core + both providers, Email, Storage, Shared
Admin.WebService               ← References: Admin.Services (+ ServicesMocks in ServiceMocks config)
Client.Services                ← References: Shared
Client                         ← References: Client.Services, Shared
```

**Hard rules:**
- Client and Client.Services NEVER reference Database or any Server project
- Database NEVER references Admin.Services, Admin.WebService, or ASP.NET Core HTTP abstractions
- Admin.WebService NEVER references Database directly — always through Admin.Services
- Provider-specific EF code stays in `Database/Postgres` or `Database/SqlServer`; anything
  provider-agnostic belongs in `Database/Core`

## Hosting & Request Entry Points

Everything is one host: `src/Server/Admin/WebService/Program.cs` (~310 lines, worth reading before
touching startup). It serves four kinds of traffic, each with a different auth path:

| Entry point | Wiring | Auth |
|---|---|---|
| Blazor SSR pages (`UI/Pages/**`) | `MapRazorComponents<App>().AddInteractiveServerRenderMode()` | Identity cookies |
| REST API (`Controllers/**`) | `MapControllers()` | JWT bearer, or `X-Api-Key` |
| SignalR (`Hubs/NotificationHub`) | `MapHub<NotificationHub>("/hubs/notifications")` | JWT via `?access_token=` |
| Health | `MapHealthChecks("/health")`, `/health/ready` | `HealthEndpointAccess` gate |

**Auth scheme selection is dynamic.** The default scheme is a policy scheme named `"Smart"` whose
`ForwardDefaultSelector` picks per request: `Authorization: Bearer …` → JWT, `X-Api-Key` header →
the `ApiKey` scheme, otherwise Identity cookies. When adding an endpoint, do not hardcode a scheme
unless you intend to bypass that selection.

**Blazor render mode is interactive Server only.** WebAssembly packages are referenced but
`src/Client/DevCoreApp.Client` is not currently hosted by WebService — it is a standalone app.
Interactive Server is exactly why the per-operation unit of work below is mandatory.

Errors are shaped by `ApiExceptionHandler` (registered via `AddExceptionHandler<T>`): JSON for API
paths, falling through to the `/Error` page for the rest.

## Shared Core vs Product Code

DevCoreApp is a **template**. Downstream products (**ThreadIQ**, **Tentrie**, …) are forks of it. To keep shared template code updatable across all forks, code is split by a marker segment placed **directly above the feature**:

- **Shared / template code** → folder `.../<Layer>/Core/<Feature>/`, namespace `...<Layer>.Core.<Feature>`
  - e.g. `Admin.Services.Core.ApiKeys`, `Admin.WebService.Core.ApiKeys`, `Shared.Core.ApiKeys`
- **Product-specific code** → folder `.../<Layer>/App/<Feature>/`, namespace `...<Layer>.App.<Feature>`
  - e.g. `Admin.Services.App.Jobs`

The split applies in: `Shared`, `Database/Core`, `Admin.Services`, `Admin.WebService`, `Email/*`, `Storage/*`, `Client`, `Client.Services`. It uses **namespace + folders inside the existing assemblies** — no new `.csproj`, the dependency graph is unchanged.

**Database exception.** The provider-agnostic `Database/Core` project already *is* the shared root — do **not** double it to `Core.Core`. Shared code keeps `Server.Database.Core.<...>` as-is; product entities go under `Server.Database.Core.App.Models.<Entity>`. The `Postgres`/`SqlServer` provider projects are untouched by this split.

**Sync key.** A file's cross-repo identity is its namespace with the `DevInstance.{Product}` prefix stripped (e.g. `Server.Database.Core.Models.ApiKey`). Files with the same sync key and **no `.App.` segment** are the *same shared surface* and must stay in lockstep across DevCoreApp/ThreadIQ/Tentrie. Anything containing `.App.` is local and never synced.

> The bulk move of existing files into `Core/` folders is a separate, deferred migration. This section describes the **target** convention that all *new* code must follow now.

### Marking project-specific deviations

When a fork must edit a **shared (`Core`) file in place** (the change can't go in an `App/` file), fence it so upstream updates don't overwrite it and it never gets ported back:

```csharp
#region project-specific Tentrie: <reason>
... deviating code ...
#endregion
```

`project-specific` is the greppable keyword. `#region` is C#-only; non-C# shared files use the same keyword with a matching close in their comment syntax:

- Razor: `@* project-specific Tentrie: reason *@` … `@* /project-specific *@`
- JSON: `// project-specific …` … `// /project-specific`
- SQL: `-- project-specific …` … `-- /project-specific`
- XML / `.csproj`: `<!-- project-specific … -->` … `<!-- /project-specific -->`

Shared-ness is expressed by *location* (`Core/`), so the tag only ever marks the **exception** — a local override inside shared code.

## Cross-Project Sync (migration in/out)

Fixes to shared `Core` code travel between repos as **instruction docs** under `docs/migration/`, not raw patches (each fork has a different product namespace prefix). DevCoreApp is the **hub** and canonical owner of `Core`; forks are spokes.

- **`docs/migration/in/`** — pending instruction docs authored elsewhere, to apply here. Archive to `in/applied/` and set `status: applied` once done.
- **`docs/migration/out/`** — instruction docs authored here for the other repos to consume into their `in/`.

Flow: a fork's `out` → DevCoreApp's `in` (upstream); apply to canonical `Core`; DevCoreApp's `out` → every fork's `in` (fan-out). Full workflow, naming, and template: [`docs/migration/README.md`](docs/migration/README.md).

## Feature Documentation

Deep-dive docs for individual features and subsystems live in [`docs/`](docs/):

- [API Keys](docs/ApiKeys.md) · [Background Tasks](docs/BackgroundTasks.md) · [Email System](docs/EmailSystem.md) · [Feature Flags](docs/FeatureFlags.md)
- [Health Checks](docs/HealthChecks.md) · [Operation Context](docs/OperationContext.md) · [Settings](docs/Settings.md) · [Webhooks](docs/Webhooks.md)
- [Specification](docs/Specification.md) — overall product spec

Subsystem guides also live next to the code: [`src/Server/Database/UnitOfWork.md`](src/Server/Database/UnitOfWork.md), [`src/Server/Storage/FileStorage.md`](src/Server/Storage/FileStorage.md), [`src/Server/Admin/Services/ImportExport/ImportExport.md`](src/Server/Admin/Services/ImportExport/ImportExport.md), [`src/Server/Admin/WebService/UI/Components/HDataGrid.md`](src/Server/Admin/WebService/UI/Components/HDataGrid.md), and the WebService-specific [`src/Server/Admin/WebService/CLAUDE.md`](src/Server/Admin/WebService/CLAUDE.md).

## Naming Conventions

- **PascalCase everywhere** — tables, columns, classes, properties, DTOs. No underscores.
- **ViewModels:** `{Entity}Item` (e.g., `UserProfileItem`, `InvoiceItem`)
- **Service interface:** `I{Entity}Service` / **Implementation:** `{Entity}Service` / **Mock:** `{Entity}ServiceMock`
- **Decorators:** `{Entity}Decorators` — extension methods `ToView()` (entity → ViewModel) and `ToRecord()` (ViewModel → entity)
- **Query classes:** `{Entity}Query` or `Core{Entity}Query`
- **Permissions:** `Module.Entity.Action` format (e.g., `Sales.Invoice.Approve`)
- ASP.NET Identity tables keep default names (`AspNetUsers`, `AspNetRoles`, etc.)

## Entity Base Classes

All entities inherit from one of three base classes in `Database/Core/Models/Base/`:

```
DatabaseBaseObject
├── Id (Guid) — internal PK, never exposed to client

DatabaseObject : DatabaseBaseObject
├── PublicId (string) — client-facing ID, generated via IdGenerator.New()
├── CreateDate (DateTime)
├── UpdateDate (DateTime)
└── IsActive (bool, default true)

DatabaseEntityObject : DatabaseObject
├── CreatedById / CreatedBy (→ UserProfile)
└── UpdatedById / UpdatedBy (→ UserProfile)
```

Orthogonal to these: implement **`IOrganizationScoped`** (adds `OrganizationId`) on any business
entity that must be caught by the organization global query filter.

- **Use `DatabaseBaseObject`** for infrastructure tables (AuditLogs, Settings)
- **Use `DatabaseObject`** for entities exposed via API but without user tracking
- **Use `DatabaseEntityObject`** for business entities that track who created/modified them

**The `Id` (Guid) never leaves the server.** APIs use `PublicId`. Decorators map `PublicId` → `ModelItem.Id` on ViewModels.

## Data Access Pattern

Services NEVER call `DbContext` directly. All data access goes through query classes.

**Per-operation unit of work (Blazor Server concurrency safety).** A Blazor interactive-Server
circuit shares ONE DI scope, so a scoped `DbContext` is shared by every component on the page.
Components initialize concurrently, so two of them querying at once run two operations on one
context → EF/Npgsql throw *"A second operation was started on this context instance"* /
*"Connection is not open"*. The fix: each service method opens its **own** short-lived context
from a factory. Inject `IQueryRepositoryFactory` (exposed as `BaseService.RepositoryFactory`) and
open one unit of work per method:

```
Service method
  → await using var repo = RepositoryFactory.Create();   // owns a fresh short-lived context
    → repo.Get{Entity}Query(AuthorizationContext.CurrentProfile)
      → returns query class implementing IModelQuery<T,D>
        → supports .Top(), .Page(), .Search(), .Sort() via IQPageable, IQSearchable, IQSortable
```

- **One `repo` per public method** — every query in the method shares it, so read → create →
  `SaveChangesAsync` stays one unit of work. Private data-touching helpers take an
  `IQueryRepository repo` parameter (callers pass theirs); never open a second `repo` in a helper.
- Background workers, seeders, and auth-pipeline code that already run one operation per DI scope
  may keep the scoped `IQueryRepository`.
- Full rationale, infrastructure, and service-writing rules: [`src/Server/Database/UnitOfWork.md`](src/Server/Database/UnitOfWork.md).

**Decorators** convert between entities and ViewModels. They are extension methods, not services:
- `entity.ToView()` → returns `{Entity}Item` ViewModel
- `entity.ToRecord(dto)` → maps DTO fields onto entity

**Cross-feature queries** live in the feature that owns the primary entity. An invoice report query lives in `Database/Invoices/`, not in a separate `Reporting/` folder.

## Organization Hierarchy & Data Scoping

Data is scoped by **Organization**, not by Tenant. `Tenant` is a thin deployment-level record (one per database — license, plan, subdomain). `Organization` is a hierarchical tree for data isolation.

```
Tenant: "Acme Corp"
  └── Root Org: Acme Corp
        ├── East Region
        │   ├── New York Office
        │   └── Boston Office
        └── West Region
```

**All business tables have `OrganizationId`.** EF Core global query filter automatically restricts queries to the user's visible organizations.

**Users connect to organizations via `UserOrganizations`:**
- `Scope = Self` → sees only that organization's data
- `Scope = WithChildren` → sees that organization + all descendants

**`IOperationContext`** provides the resolved context to the data layer:
- `UserId`, `PrimaryOrganizationId`, `VisibleOrganizationIds`, `IpAddress`, `CorrelationId`
- Two implementations, chosen by a DI factory lambda based on whether an `HttpContext` exists:
  `HttpOperationContext` (WebService — reads org values from claims) and `BackgroundOperationContext`
  (Services — mutable, populated per background job)
- Database project depends on this interface, NOT on `IHttpContextAccessor`
- `ApplicationDbContext` applies the global filter to every entity implementing `IOrganizationScoped`
  — implement that interface on new business entities to get scoping for free

**When creating new records**, set `OrganizationId` to `IOperationContext.PrimaryOrganizationId`.

## Permissions System

ASP.NET Identity handles roles. DevCoreApp adds a permission layer on top via claims transformation.

**Flow:**
1. User logs in → Identity loads roles from `AspNetUserRoles`
2. `PermissionClaimsTransformation` (`IClaimsTransformation`) resolves role → permission mappings from `RolePermissions` table
3. Checks `UserPermissionOverrides` for per-user grants/denials
4. Injects `Permission:Module.Entity.Action` claims into `ClaimsPrincipal`
5. `[Authorize(Policy = "Sales.Invoice.Approve")]` checks for the claim

**Policies are never registered explicitly.** `PermissionPolicyProvider` (a custom
`IAuthorizationPolicyProvider`) builds a policy on demand from any policy name it is asked for, so a
new permission needs **no** `AddPolicy` call — just the constant, the seeded row, and the attribute.

**Permission keys use `Module.Entity.Action` format.** Define them as constants in
`PermissionDefinitions` (`src/Shared/Model/Permissions/PermissionDefinitions.cs`), which
`PermissionSeeder` reflects over to populate the `Permissions` table:
```csharp
public static class Sales
{
    public static class Invoice
    {
        public const string View = "Sales.Invoice.View";
        public const string Approve = "Sales.Invoice.Approve";
    }
}
```

**Use permissions, not roles, for authorization checks.** `[Authorize(Roles = "Admin")]` is acceptable for broad checks, but feature-level access must use `[Authorize(Policy = "...")]`.

## Audit Logging

**Dual mechanism:**
- **EF Core `SaveChangesInterceptor`** (`Database/Core/Data/AuditInterceptor.cs`) — catches all changes through the application. Has full user context. This is the primary mechanism.
- **Database triggers** — on critical tables only. Catches changes from any source. No user context available. Currently **Postgres only**: `audit_trigger_function()` is attached to `AspNetUsers` and `Organizations` via `AuditTriggerExtensions` helpers in `Database/Postgres/Migrations/`. The SqlServer provider has no trigger equivalent yet.

Both write to the same `AuditLogs` table, distinguished by the `AuditSource` enum on the `Source` column (Application vs Database).

**Sensitive fields** decorated with `[AuditExclude]` are omitted from audit values (e.g., `PasswordHash`, `SecurityStamp`).

## Background Jobs

The worker runs **in-process** inside WebService: `BackgroundWorker` is registered as a singleton
`AddHostedService`, and `BackgroundTaskWorker` does the claiming/execution. There is no separate
worker host project.

**The database is the source of truth.** `BackgroundWorker.SubmitAsync` persists a `BackgroundTask`
row *before* anything runs; the in-memory queue is only a local wake-up optimization, so jobs
survive process restart.

**Flow:** submit `BackgroundRequestItem` → persist `BackgroundTask` (`Queued`) → worker claims it by
atomically flipping status to `Running` → dispatches to the matching `IBackgroundTaskHandler` →
`Completed`, or a `BackgroundTaskLog` attempt row + requeue with backoff / `Failed`.

- Handlers implement `IBackgroundTaskHandler` and are registered as singletons in `Program.cs`
  (`SendEmailTaskHandler`, `ImportDataTaskHandler`, `WebhookDeliveryTaskHandler`).
- Handler code runs outside an HTTP request, so it uses `BackgroundOperationContext` rather than
  `HttpOperationContext` for `IOperationContext`.
- **`ResultReference`** links a task to its domain entity (e.g., `EmailLog:abc-123`). Domain tables
  own business state; `BackgroundTasks` owns execution state.
- Full design: [`docs/BackgroundTasks.md`](docs/BackgroundTasks.md).

## File Storage

Provider-based file storage with local disk (default) and S3 (stub). Configuration and usage details: [`src/Server/Storage/FileStorage.md`](src/Server/Storage/FileStorage.md).

**Quick reference:** Files are uploaded via `IFileService.UploadAsync()`, metadata stored in `FileRecords` table (organization-scoped), physical files stored by `IFileStorageProvider`. Provider is registered in `Program.cs` via `AddLocalFileStorage()`. Runtime limits (max size, allowed types, soft-delete) are managed via the Settings table under the `Storage` category.

## Exception Handling

- Controllers use `HandleWebRequestAsync()` from WebServiceToolkit
- Use WebServiceToolkit exception types: `BadRequestException` (400), `UnauthorizedException` (401), `RecordNotFoundException` (404), `RecordConflictException` (409)
- Use `BusinessRuleException` for domain validation failures (422)
- Do NOT throw generic `Exception` or `InvalidOperationException` for expected error cases

## Feature Folder Organization

`Admin.Services`, `Shared/Model`, and the WebService UI use **vertical slices** — group by feature,
not by technical layer:

```
Admin.Services/Invoices/        InvoiceService.cs, InvoiceValidator.cs
Shared/Model/Invoices/          InvoiceItem.cs, InvoiceCreateRequest.cs
Admin.WebService/UI/Pages/…     InvoiceListPage.razor, InvoiceDetailPage.razor
Admin.WebService/Controllers/   InvoiceController.cs
```

**`Database/Core` is the exception — it is grouped by kind, not by feature.** Match the existing
layout when adding an entity:

```
Database/Core/
├── ApplicationDbContext.cs              # Org global query filter (IOrganizationScoped), model config
├── Models/                              # Entities: Organization.cs, ApiKey.cs, …
│   ├── Base/                            # DatabaseBaseObject / DatabaseObject / DatabaseEntityObject
│   └── <Feature>/                       # Sub-folder only when a feature has several entities
└── Data/
    ├── Queries/IInvoiceQuery.cs                        # Query interface
    ├── Queries/BasicsImplementation/CoreInvoiceQuery.cs # Implementation (Core prefix)
    ├── Decorators/InvoiceDecorators.cs                  # ToView() / ToRecord() extensions
    ├── IQueryRepository.cs / IQueryRepositoryFactory.cs # Unit-of-work seam
    ├── IOperationContext.cs / AuditInterceptor.cs
    └── IDataSeeder.cs
```

A new query must be added to `IQueryRepository` (as a `Get{Entity}Query(...)` method) and
implemented in `CoreQueryRepository` before services can reach it.

## Things To Always Do

- Run `dotnet build` before committing to verify compilation
- Use `IdGenerator.New()` for PublicId values, never `Guid.NewGuid().ToString()`
- Use `query.CreateNew()` to instantiate entities — never `new Entity { ... }` directly. The query's `CreateNew()` method sets `Id`, `PublicId`, `CreateDate`, `UpdateDate` (and other base fields) consistently via `IdGenerator` and `ITimeProvider`. The only exception is data seeders that run during database initialization.
- Use LogScope (`IScopeLog`) for logging, not `ILogger`
- Use `[AuditExclude]` on sensitive entity properties
- Set `OrganizationId` on new business records
- Return `ServiceActionResult<T>` from services, not raw values or exceptions
- Use `ModelList<T>` for paginated responses
- Put shared/template code under a `Core` segment and product-specific code under `App` (see [Shared Core vs Product Code](#shared-core-vs-product-code))

## Things To Never Do

- Never expose `Id` (Guid PK) to the client — use `PublicId`
- Never call `DbContext` directly from a service — use query classes
- Never share one `DbContext`/repository across a Blazor circuit — open a per-operation `await using var repo = RepositoryFactory.Create();` in each service method (see Data Access Pattern → [`UnitOfWork.md`](src/Server/Database/UnitOfWork.md)). Never inject the scoped `ApplicationDbContext` into a Blazor-facing service.
- Never instantiate entities directly with `new Entity { ... }` — use `query.CreateNew()` instead (seeders are the only exception)
- Never inject `DbContext` or database types into pages or controllers
- Never add ASP.NET Core HTTP dependencies to the Database project
- Never put business logic in entity classes — keep them as data models
- Never create `InputModel` classes in pages — DTOs carry validation attributes
- Never bypass the organization scoping filter with `IgnoreQueryFilters()` unless explicitly required for admin/system operations
- Never use `ILogger` / `LogInformation` — use `IScopeLog` from DevInstance.LogScope
- Never create or scaffold EF Core migrations (`dotnet ef migrations add`) — notify the user that a migration is needed and let them create it
- Never edit shared `Core` code in place for a product-only change without fencing it in a `#region project-specific <Product>:` block (or the non-C# equivalent) — see [Marking project-specific deviations](#marking-project-specific-deviations)
