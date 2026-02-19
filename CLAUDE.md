# CLAUDE.md — DevCoreApp Solution Guide

This is the root-level guide for the entire DevCoreApp solution. Project-specific conventions (page patterns, service patterns, mocks) are in `src/Server/Admin/WebService/CLAUDE.md`.

## What Is This Project?

DevCoreApp is a reusable starter template for custom ERP and CRM applications. It provides user management, permissions, organization hierarchy, background jobs, email, notifications, and file storage out of the box. Each new project forks this template and builds domain features on top.

## Tech Stack

- .NET 10+, ASP.NET Core, Entity Framework Core, ASP.NET Identity
- Blazor SSR (Admin UI), Blazor WebAssembly (field worker client)
- PostgreSQL (primary), SQL Server (secondary)
- DevInstance.BlazorToolkit — client-side Blazor utilities (`[BlazorService]`, `IApiContext<T>`, `IServiceExecutionHost`)
- DevInstance.WebServiceToolkit — server-side utilities (`[WebService]`, `[QueryModel]`, `ModelItem`, `ModelList<T>`, `HandleWebRequestAsync()`, `IModelQuery<T,D>`)
- DevInstance.LogScope — scope-based logging (`IScopeManager`, `IScopeLog`)

## Solution Structure

```
DevCoreApp/
├── src/
│   ├── Client/
│   │   ├── DevCoreApp.Client/                 # Blazor WASM app
│   │   └── DevCoreApp.Client.Services/        # Client-side API clients
│   ├── Server/
│   │   ├── Admin/
│   │   │   ├── DevCoreApp.Admin.Services/     # Business logic, auth, notifications
│   │   │   └── DevCoreApp.Admin.WebService/   # Blazor SSR host + API controllers + SignalR
│   │   ├── DevCoreApp.Worker/                 # Background job worker (separate host)
│   │   ├── DevCoreApp.Database/               # Entities, queries, decorators, EF config
│   │   ├── DevCoreApp.Email/                  # Email provider implementations
│   │   └── DevCoreApp.Storage/                # File storage provider implementations
│   └── Shared/                                # ViewModels, constants, enums
├── mocks/
│   └── Server/Admin/ServicesMocks/            # Mock services for UI development
├── tests/                                     # Mirrors src/ structure
└── docs/
```

## Dependency Rules — Do Not Violate

```
DevCoreApp.Shared              ← Referenced by everything. No project dependencies.
DevCoreApp.Database            ← References: Shared
DevCoreApp.Email               ← References: Shared
DevCoreApp.Storage             ← References: Shared
DevCoreApp.Admin.Services      ← References: Database, Email, Storage, Shared
DevCoreApp.Admin.WebService    ← References: Admin.Services, Shared
DevCoreApp.Worker              ← References: Admin.Services, Database, Shared
DevCoreApp.Client.Services     ← References: Shared
DevCoreApp.Client              ← References: Client.Services, Shared
```

**Hard rules:**
- Client and Client.Services NEVER reference Database or any Server project
- Database NEVER references Admin.Services, Admin.WebService, or ASP.NET Core HTTP abstractions
- Admin.WebService NEVER references Database directly — always through Admin.Services
- Worker is a separate hosted process from WebService. Do not merge them.

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
└── UpdateDate (DateTime)

DatabaseEntityObject : DatabaseObject
├── CreatedBy (→ UserProfile) — navigation property
└── UpdatedBy (→ UserProfile) — navigation property
```

- **Use `DatabaseBaseObject`** for infrastructure tables (AuditLogs, JobLogs, Settings)
- **Use `DatabaseObject`** for entities exposed via API but without user tracking
- **Use `DatabaseEntityObject`** for business entities that track who created/modified them

**The `Id` (Guid) never leaves the server.** APIs use `PublicId`. Decorators map `PublicId` → `ModelItem.Id` on ViewModels.

## Data Access Pattern

Services NEVER call `DbContext` directly. All data access goes through query classes.

```
Service
  → Repository.Get{Entity}Query(AuthorizationContext.CurrentProfile)
    → returns query class implementing IModelQuery<T,D>
      → supports .Top(), .Page(), .Search(), .Sort() via IQPageable, IQSearchable, IQSortable
```

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
- Populated from HTTP context in WebService, from job context in Worker
- Database project depends on this interface, NOT on `IHttpContextAccessor`

**When creating new records**, set `OrganizationId` to `IOperationContext.PrimaryOrganizationId`.

## Permissions System

ASP.NET Identity handles roles. DevCoreApp adds a permission layer on top via claims transformation.

**Flow:**
1. User logs in → Identity loads roles from `AspNetUserRoles`
2. `IClaimsTransformation` resolves role → permission mappings from `RolePermissions` table
3. Checks `UserPermissionOverrides` for per-user grants/denials
4. Injects `Permission:Module.Entity.Action` claims into `ClaimsPrincipal`
5. `[Authorize(Policy = "Sales.Invoice.Approve")]` checks for the claim

**Permission keys use `Module.Entity.Action` format.** Define them as constants in `PermissionDefinitions`:
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
- **EF Core `SaveChangesInterceptor`** — catches all changes through the application. Has full user context. This is the primary mechanism.
- **Database triggers** — on critical tables only (financial records, user credentials, permission tables). Catches changes from any source. No user context available.

Both write to the same `AuditLogs` table with a `Source` column (Application vs Database).

**Sensitive fields** decorated with `[AuditExclude]` are omitted from audit values (e.g., `PasswordHash`, `SecurityStamp`).

## Background Jobs

Jobs are persisted to the `Jobs` table BEFORE the worker picks them up. This prevents job loss on process restart.

**Flow:** Create `Job` record → Worker picks up → Creates `JobLogs` entry per attempt → Updates `Job` status

**`ResultReference`** links a job to its domain entity (e.g., `EmailLog:abc-123`). Domain tables own business state; `Jobs` owns execution state.

**The Worker (`DevCoreApp.Worker`) runs as a separate process.** It can be deployed and restarted independently from WebService.

## Exception Handling

- Controllers use `HandleWebRequestAsync()` from WebServiceToolkit
- Use WebServiceToolkit exception types: `BadRequestException` (400), `UnauthorizedException` (401), `RecordNotFoundException` (404), `RecordConflictException` (409)
- Use `BusinessRuleException` for domain validation failures (422)
- Do NOT throw generic `Exception` or `InvalidOperationException` for expected error cases

## Feature Folder Organization

Each project uses vertical slices — group by feature, not by technical layer:

```
Database/
├── Invoices/
│   ├── Invoice.cs              # Entity
│   ├── InvoiceConfiguration.cs # EF config
│   ├── InvoiceQuery.cs         # Query implementation
│   └── InvoiceDecorator.cs     # Entity ↔ ViewModel mapper

Admin.Services/
├── Invoices/
│   ├── InvoiceService.cs       # Business logic
│   └── InvoiceValidator.cs     # Validation rules

Admin.WebService/
├── Invoices/
│   ├── InvoiceController.cs    # API endpoints
│   ├── InvoiceListPage.razor   # Admin UI
│   └── InvoiceDetailPage.razor

Shared/
├── Invoices/
│   ├── InvoiceItem.cs          # ViewModel
│   └── InvoiceCreateRequest.cs # Request DTO
```

## Things To Always Do

- Run `dotnet build` before committing to verify compilation
- Use `IdGenerator.New()` for PublicId values, never `Guid.NewGuid().ToString()`
- Use LogScope (`IScopeLog`) for logging, not `ILogger`
- Use `[AuditExclude]` on sensitive entity properties
- Set `OrganizationId` on new business records
- Return `ServiceActionResult<T>` from services, not raw values or exceptions
- Use `ModelList<T>` for paginated responses

## Things To Never Do

- Never expose `Id` (Guid PK) to the client — use `PublicId`
- Never call `DbContext` directly from a service — use query classes
- Never inject `DbContext` or database types into pages or controllers
- Never add ASP.NET Core HTTP dependencies to the Database project
- Never put business logic in entity classes — keep them as data models
- Never create `InputModel` classes in pages — DTOs carry validation attributes
- Never bypass the organization scoping filter with `IgnoreQueryFilters()` unless explicitly required for admin/system operations
- Never use `ILogger` / `LogInformation` — use `IScopeLog` from DevInstance.LogScope
