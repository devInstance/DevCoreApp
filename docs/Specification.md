# DevCoreApp — Starter Template Specification

## Overview

DevCoreApp is a reusable project template designed to serve as the foundation for custom ERP and CRM applications. Built on ASP.NET Core with Blazor SSR (admin) and Blazor WebAssembly (mobile), it provides essential infrastructure, security, and operational capabilities out of the box so that each new project starts with a production-ready baseline.

**Tech Stack:**
- ASP.NET Core (.NET 10+)
- Blazor Server-Side Rendering (Admin UI)
- Blazor WebAssembly (Mobile / Client Apps)
- PostgreSQL (primary) / SQL Server (secondary support)
- Entity Framework Core
- ASP.NET Identity (authentication foundation)
- Background Worker Service
- DevInstance.BlazorToolkit (Blazor client-side utilities)
- DevInstance.WebServiceToolkit (ASP.NET Core web service utilities)
- DevInstance.LogScope (structured scope-based logging)

---

## devInstance Libraries

DevCoreApp builds on top of three in-house NuGet packages from devInstance LLC. These libraries define the core development patterns used throughout the codebase. Understanding them is essential for working in this project.

### DevInstance.BlazorToolkit

NuGet: `DevInstance.BlazorToolkit`

Provides client-side Blazor utilities used in `DevCoreApp.Client` and `DevCoreApp.Client.Services`:

- **`[BlazorService]` attribute** — Marks service classes for automatic DI registration via `AddBlazorServices()`. Supports Scoped (default), Singleton, and Transient lifetimes.
- **`IApiContext<T>`** — Typed REST API client with fluent interface for building queries (`.Get()`, `.Top()`, `.Page()`, `.ListAsync()`).
- **`ServiceActionResult<T>`** — Standardized result wrapper for service calls, used with `ServiceUtils.HandleWebApiCallAsync()`.
- **`IServiceExecutionHost`** — Cascading parameter interface for managing loading/error states in Blazor pages.
- **`BlazorToolkitPageLayout`** — Layout component that implements loading and error handling states automatically.
- **Form validators** — Reusable validation components for Blazor forms.

### DevInstance.WebServiceToolkit

NuGet: `DevInstance.WebServiceToolkit`, `DevInstance.WebServiceToolkit.Common`, `DevInstance.WebServiceToolkit.Database`

Provides server-side ASP.NET Core utilities used in `DevCoreApp.Admin.WebService`, `DevCoreApp.Admin.Services`, and `DevCoreApp.Database`:

- **`[WebService]` attribute** — Marks service classes for automatic DI registration via `AddServerWebServices()`. Equivalent of `[BlazorService]` for the server side.
- **`[QueryModel]` / `[QueryName]` attributes** — Automatic query string binding to strongly-typed POCO classes for API endpoints.
- **`ModelItem`** — Base class with `Id` property for all entities/ViewModels.
- **`ModelList<T>`** — Standardized paginated, sortable, searchable collection response DTO.
- **`HandleWebRequestAsync()`** — Controller extension method that wraps actions with standardized exception-to-HTTP-status mapping.
- **HTTP exception types** — `BadRequestException` (400), `UnauthorizedException` (401), `RecordNotFoundException` (404), `RecordConflictException` (409). These are used instead of custom exception types.
- **`IModelQuery<T,D>`** — Base query interface with CRUD operations (the foundation for all query classes in `DevCoreApp.Database`).
- **`IQPageable<T>`**, **`IQSearchable<T,K>`**, **`IQSortable<T>`** — Fluent interfaces for building paginated, searchable, and sortable database queries.

### DevInstance.LogScope

NuGet: `DevInstance.LogScope`, `DevInstance.LogScope.NET`

Lightweight scope-based logging framework. Provides `IScopeManager` and `IScopeLog` for structured tracing and profiling with hierarchical scope context. Integrates with Microsoft.Extensions.Logging via `DevInstance.LogScope.Extensions.MicrosoftLogger`.

---

## Naming Convention

All names use PascalCase without underscores — tables, columns, classes, properties, and DTOs.

- Tables: `AuditLogs`, `RolePermissions`, `EmailTemplates`
- Columns: `OrganizationId`, `CreateDate`, `ChangedByUserId`
- Classes: `ApplicationUser`, `EmailTemplate`, `JobService`

---

## Priority Tiers

Features are organized into three tiers based on how difficult they are to retrofit and how broadly they impact the codebase.

- **P0 — Must have before first project.** These touch nearly every table, component, or endpoint. Retrofitting is expensive.
- **P1 — Should have before first project.** Important for production readiness but can be layered in with moderate effort.
- **P2 — Nice to have.** Add as needed per project. The template should define the pattern/abstraction but full implementation can wait.

---

## P0 — Core Foundation

### 1. User Management

The system must support full user lifecycle management. Built on top of ASP.NET Identity's `IdentityUser` and `IdentityDbContext`.

**Identity Integration:**
- Subclass `IdentityUser<Guid>` → `ApplicationUser` with organization assignment, profile fields, and custom properties
- Subclass `IdentityRole<Guid>` → `ApplicationRole` with `Description`
- Subclass `IdentityDbContext<ApplicationUser, ApplicationRole, Guid>` → `ApplicationDbContext`
- ASP.NET Identity owns: password hashing, lockout, token generation, cookie auth, JWT auth
- DevCoreApp extends: organization scoping, profile data, permission system, login history

**Requirements:**
- User registration (admin-created and self-registration toggle)
- Login / logout with cookie-based authentication (Blazor SSR) and JWT (API / WASM)
- Password reset flow via email
- Account lockout after configurable failed attempts
- User profile (name, email, phone, avatar, timezone, preferences)
- User status: Active, Inactive, Locked, Pending Approval
- "Last login" and "login history" tracking
- Impersonation support for admin troubleshooting (with audit trail)
- Users assigned to one or more organizations via `UserOrganizations`

**Database Tables:**
- `AspNetUsers` — Identity's user table (extended via `ApplicationUser` with `Status`, `LastLoginAt`, `PrimaryOrganizationId`)
- `AspNetRoles` — Identity's role table (extended via `ApplicationRole` with `Description`, `IsSystemRole`)
- `AspNetUserRoles` — Identity's user-role mapping (used as-is)
- `UserProfiles` — Extended profile data (avatar, phone, timezone, preferences as jsonb)
- `UserSessions` — Active session tracking
- `UserLoginHistory` — Login attempts log

---

### 2. Roles & Permissions

ASP.NET Identity handles role assignment via `AspNetUserRoles`. DevCoreApp extends this with a granular permission layer that resolves at login time through claims transformation.

**Architecture — How Identity and Permissions Work Together:**

1. User logs in → Identity loads their roles from `AspNetUserRoles` as normal
2. A custom `IClaimsTransformation` fires → reads `RolePermissions` table → resolves all permissions for the user's roles
3. Checks `UserPermissionOverrides` for any per-user grants or denials
4. Injects permission claims (e.g., `Permission:Sales.Invoice.Approve`) into the `ClaimsPrincipal`
5. Also resolves the user's visible `OrganizationId` set and injects as claims
6. Custom `AuthorizationPolicyProvider` auto-generates policies from permission strings
7. `[Authorize(Policy = "Sales.Invoice.Approve")]` checks for the matching claim

This means Identity's built-in methods (`UserManager.IsInRoleAsync()`, `UserManager.AddToRoleAsync()`, `[Authorize(Roles = "Admin")]`) all continue to work normally. Roles flow through Identity. Permissions are an additional layer resolved from roles at authentication time.

**Requirements:**
- Permissions defined as string keys: `Module.Entity.Action` (e.g., `Sales.Invoice.Approve`, `Admin.Users.Delete`)
- Roles are collections of permissions (e.g., "Sales Manager" = 15 permissions)
- Users can have multiple roles (via Identity's `AspNetUserRoles`)
- Users can have direct permission overrides (grant or deny) independent of roles
- System roles (Admin, User) that cannot be deleted (`IsSystemRole = true`)
- Custom roles created per deployment
- Permission checks available as: `[Authorize(Policy = "...")]`, Blazor component attribute, programmatic `IPermissionService.HasPermission()`
- UI dynamically hides/shows elements based on permissions
- Permissions cached per user session, invalidated on role/permission change

**Database Tables (DevCoreApp custom, alongside Identity tables):**
- `Permissions` — Permission definitions
  - `Id`, `Module`, `Entity`, `Action`, `Key` (computed: `Module.Entity.Action`), `Description`, `DisplayOrder`
- `RolePermissions` — Many-to-many: `ApplicationRole` → `Permission`
  - `RoleId`, `PermissionId`
- `UserPermissionOverrides` — Direct user-level grants/denials
  - `Id`, `UserId`, `PermissionId`, `IsGranted` (true = grant, false = deny), `Reason`

**Seed Data:**
- Default Admin role with all permissions
- Default User role with basic read permissions
- Full permission list generated from module definitions

---

### 3. Audit Logging

Every data change in the system must be tracked. This is non-negotiable for business applications.

**Design Decisions:**

**Single `AuditLogs` table** — not per-table audit tables. With 50+ entities in a typical ERP, per-table audit tables create unmanageable schema bloat. A single table with `TableName`, `RecordId`, and jsonb `OldValues`/`NewValues` handles all entities. When a column is added to a business table, the audit table doesn't change — the new field appears in the JSON automatically. Performance at scale is handled by indexing and optional partitioning by `TableName` at the database level.

**Dual audit mechanism** — EF Core interceptor as the primary mechanism, database triggers on critical tables only.

| Aspect | EF Core Interceptor | Database Trigger |
|--------|---------------------|------------------|
| Scope | All tables automatically | Critical tables only (opt-in) |
| User context | Full (user, IP, correlation ID, organization) | None (no HTTP context) |
| Catches bypass | No — misses raw SQL, DBA changes, external scripts | Yes — fires on all DML regardless of source |
| Maintenance | C# code, easy to test | SQL, harder to maintain |
| Performance | One extra insert per `SaveChanges` call | One extra insert per row per DML |
| When to use | Default for everything | Financial, security, compliance tables |

The EF Core `SaveChangesInterceptor` catches everything that goes through the application's `DbContext.SaveChangesAsync()`. It has full access to the application context — who the user is, the correlation ID, the IP address, which organization they're in.

Database triggers catch everything — including raw SQL, bulk operations via `ExecuteSqlRaw()`, other applications or scripts writing to the same database, DBA manual fixes in production, and database-level cascades. They are applied only to tables where tamper-proof auditing is required (financial records, user credentials, permission tables). A trigger-generated entry with no `ChangedByUserId` is itself the important information: something changed this data outside the application.

Both mechanisms write to the same `AuditLogs` table with a `Source` column that distinguishes the origin. If you see a `Database` source entry for a table that normally goes through the application, that's a red flag worth investigating.

**Requirements:**
- Automatic capture of all INSERT, UPDATE, DELETE operations on all business entities via EF Core interceptor
- Database triggers on designated critical tables for tamper-proof auditing
- Store: who, when, what table, what record, old values, new values, source
- Audit log is append-only — no updates or deletes allowed
- Queryable audit trail per entity (e.g., "show me all changes to Invoice #1042")
- Retention policy: configurable per table (e.g., keep financial audits 7 years, keep session logs 90 days)
- Exclude sensitive fields from audit via `[AuditExclude]` attribute (e.g., password hashes, security stamps)
- Audit entries written in the same transaction as the business data change
- Composite index on `(TableName, RecordId, ChangedAt)` for efficient per-entity queries
- Partitioning by `TableName` available at the database level if audit volume grows

**Database Tables:**
- `AuditLogs` — Primary audit trail (single table for all entities)
  - `Id`, `OrganizationId`, `TableName`, `RecordId`, `Action` (Insert/Update/Delete), `OldValues` (jsonb), `NewValues` (jsonb), `ChangedByUserId` (null for trigger-sourced entries), `ChangedAt`, `IpAddress`, `CorrelationId`, `Source` (Application/Database)

**Enums:**
- `AuditAction`: `Insert`, `Update`, `Delete`
- `AuditSource`: `Application` (from EF Core interceptor), `Database` (from trigger)

**Sensitive Field Exclusion:**
- Properties decorated with `[AuditExclude]` are omitted from `OldValues`/`NewValues`
- Applied by default to `PasswordHash`, `SecurityStamp`, and any field marked sensitive

**Critical Tables (trigger-audited):**
- The set of tables that receive database triggers is configurable per deployment
- Recommended defaults: financial records (invoices, payments, ledger entries), user credentials (`AspNetUsers`), permission tables (`RolePermissions`, `UserPermissionOverrides`), organization structure (`Organizations`)
- Triggers are generated and managed via EF Core migrations alongside the trigger function

---

### 4. Tenant & Organization Hierarchy

The system uses two complementary concepts: a **Tenant** for deployment-level metadata and an **Organization hierarchy** for data scoping.

**Tenant** is a thin record representing the entire client deployment. There is exactly one Tenant per database. It holds deployment-level concerns: license/plan, subdomain, global settings, API rate limits. Business tables do **not** carry a `TenantId` — it is not used for data scoping.

**Organization** is a hierarchical tree representing the client's internal structure: company → regions → offices → divisions. All data scoping happens through `OrganizationId`. A user's visibility is determined by which organizations they are assigned to and whether their access scope includes child organizations.

**Example Structure:**
```
Tenant: "Acme Corp" (Plan: Enterprise, Subdomain: acme)
  └── RootOrganization: Acme Corp
        ├── East Region
        │   ├── New York Office
        │   └── Boston Office
        ├── West Region
        │   ├── Los Angeles Office
        │   └── Seattle Office
        └── Acquired Co (formerly separate company)
            ├── Denver Office
            └── Phoenix Office
```

**Scenarios This Handles:**
| Scenario | Solution |
|----------|----------|
| Company acquires another company | Add new Organization under root. Existing data keeps its OrganizationId — instantly visible to top management. |
| New office opens | Add Organization node under the right region. Regional manager with `WithChildren` scope automatically sees it. |
| Manager transferred to different region | Update their `UserOrganizations` record. |
| Top admin needs to see everything | One `UserOrganizations` record: root org + `WithChildren`. |
| Auditor needs read-only across all data | Root org + `WithChildren` for scope, but limited permissions (only `*.*.View`). Permissions and organization scoping work independently. |
| Single-office simple deployment | One root Organization, all users assigned to it. Hierarchy adds zero overhead. |

**Database Tables:**

- `Tenants` — Deployment metadata (one per database)
  - `Id`, `Name`, `Subdomain`, `Status`, `Plan`, `Settings` (jsonb), `RootOrganizationId`, `CreateDate`

- `Organizations` — Hierarchical org structure
  - `Id`, `Name`, `Code` (short identifier: "NYC", "EAST"), `ParentId` (null = root), `Level` (0 = root, 1 = region, 2 = office...), `Path` (materialized path: "/ACME/EAST/NYC"), `Type` ("Company", "Region", "Office", "Division"), `IsActive`, `Settings` (jsonb), `SortOrder`

- `UserOrganizations` — Maps users to their organization access
  - `Id`, `UserId`, `OrganizationId`, `Scope` (enum: `Self`, `WithChildren`), `IsPrimary` (the user's home organization for creating new records)

**Materialized Path:**

The `Path` column enables fast hierarchy queries without recursive CTEs:

```sql
-- Everything under East Region (regional manager)
WHERE Path LIKE '/ACME/EAST/%'

-- Everything under Acme Corp (top admin)
WHERE Path LIKE '/ACME/%'

-- Only New York Office (office clerk)
WHERE OrganizationId = 'ny-guid'
```

**Organization Context Service:**

At login, the system precomputes the set of all `OrganizationId` values visible to the user and stores them in `IOrganizationContext`. This set is used by the EF Core global query filter on every query.

```
IOrganizationContext
├── UserId
├── PrimaryOrganizationId      — for creating new records
└── VisibleOrganizationIds     — precomputed set of all org IDs the user can see
```

**EF Core Global Filter:**

All business entities are automatically filtered by the user's visible organizations:

```csharp
// Developer writes normal code — no awareness of hierarchy needed
var pendingInvoices = await _db.Invoices
    .Where(i => i.Status == InvoiceStatus.Pending)
    .ToListAsync();

// EF Core produces:
// SELECT * FROM Invoices
// WHERE Status = 'Pending'
//   AND OrganizationId IN ('ny-guid', 'boston-guid', 'east-guid')
```

**When to use Tenant vs Organization:**

| Need | Use |
|------|-----|
| Check license/plan | `ITenantContext.Plan` |
| Check deployment settings | `ITenantContext.Settings` |
| Resolve subdomain | `ITenantContext.Subdomain` |
| Filter business data | `OrganizationId` via EF Core global filter |
| Determine where to create a new record | `IOrganizationContext.PrimaryOrganizationId` |
| Determine what data a user can see | `IOrganizationContext.VisibleOrganizationIds` |

**Implementation Notes:**
- Middleware resolves Tenant from subdomain/header early in the pipeline, sets `ITenantContext`
- `IClaimsTransformation` resolves user's visible organizations at authentication time, populates `IOrganizationContext`
- Global query filter on all business entities uses `IOrganizationContext.VisibleOrganizationIds`
- When a new record is created, `OrganizationId` defaults to `IOrganizationContext.PrimaryOrganizationId`
- Path is rebuilt when an organization is moved in the hierarchy (rare admin operation)
- Organization tree is cached and invalidated on structural changes

---

### 5. File Storage Abstraction

Every ERP/CRM eventually needs file attachments. The abstraction must be in place from the start.

**Requirements:**
- Storage interface supporting: Upload, Download, Delete, GetUrl, Exists
- Implementations: Local filesystem (dev), S3-compatible (production), Azure Blob (optional)
- File metadata stored in database, binary stored in provider
- Virus scanning hook (interface for future integration)
- Max file size configurable per organization or tenant
- Allowed file types configurable
- Thumbnail generation for images
- Signed URL generation for time-limited access

**Database Tables:**
- `Files` — File metadata
  - `Id`, `OrganizationId`, `FileName`, `OriginalName`, `ContentType`, `SizeBytes`, `StorageProvider`, `StoragePath`, `UploadedByUserId`, `UploadedAt`, `EntityType`, `EntityId`

---

## P1 — Production Readiness

### 6. Background Job Worker

Long-running and scheduled tasks must run outside the request pipeline.

**Design Decisions:**

**`BackgroundRequestItem` dispatcher pattern.** The application queues work by creating a `BackgroundRequestItem` (which can be any typed payload). The worker picks up the item and routes it to the appropriate handler based on the job type. This keeps the queuing API simple and consistent across all job types.

**Persist before processing.** Every `BackgroundRequestItem` is persisted to the `Jobs` table before the worker picks it up. This prevents job loss on process restart — if the worker crashes, in-memory queues are lost but the `Jobs` table survives. On startup, the worker scans for `Status = Running` jobs (they crashed mid-execution) and resets them to `Queued` for reprocessing.

**`Jobs` table is the queue and the history.** A single table serves as both the work queue (filter by `Status = Queued`) and the execution history (filter by `Status = Completed/Failed`). No separate queue infrastructure needed.

**`ResultReference` bridges jobs to domain entities.** The `Jobs` table does not duplicate domain state. Instead, it stores a `ResultReference` string that points to the domain entity holding the business result. For email, this points to an `EmailLog` record. For a report, it points to a `Files` record. Domain tables own business state; the `Jobs` table owns execution state.

**`JobLogs` for execution attempt history.** Each time a job is attempted (including retries), a `JobLogs` entry is created with the outcome. This provides a structured per-job execution timeline visible in the admin dashboard without searching through Serilog. For example, a job that failed twice then succeeded has three `JobLogs` entries showing each attempt's error or success.

**Example flow — sending email:**

1. Application creates `EmailLog` (Status: Queued) and `Job` (Type: `SendEmail`, Payload: `{EmailLogId: "..."}`, ResultReference: `EmailLog:abc-123`)
2. Worker picks up `Job`, updates Status to `Running`, creates `JobLogs` entry (Attempt: 1, Status: Running)
3. Handler sends email, updates `EmailLog` with result
4. Worker updates `Job` to `Completed`, updates `JobLogs` entry with outcome
5. If failed: worker updates `Job` to `Failed`, increments `RetryCount`, schedules retry with backoff, `JobLogs` entry records the error
6. On manual retry: user queues a new `Job` pointing to the same `EmailLogId`

**Example flow — generate monthly report (no domain state table):**

1. Application creates `Job` (Type: `GenerateMonthlyReport`, Payload: `{Month: "2026-01", CustomerId: "..."}`)
2. Worker picks up `Job`, processes report, saves PDF via file storage
3. Worker updates `Job` with ResultReference: `Files:generated-file-id`, Status: `Completed`
4. Admin dashboard shows the job completed and links to the generated file

**Requirements:**
- Hosted service based on `BackgroundService` / `IHostedService`
- Job queue stored in PostgreSQL (no external dependency like Redis required for basic usage)
- Job types: Immediate (fire-and-forget), Delayed (run at specific time), Recurring (cron expression)
- Job status tracking: Queued, Running, Completed, Failed
- Configurable retry with exponential backoff
- Dead letter handling for permanently failed jobs (max retries exceeded)
- Crash recovery: on startup, scan for `Running` jobs and reset to `Queued`
- Job dashboard in admin UI showing status, history, execution attempts, and ability to retry/cancel
- Concurrency control: configurable max parallel jobs
- Job handlers registered via DI, routed by `JobType` string

**Database Tables:**
- `Jobs` — Job queue and execution history
  - `Id`, `OrganizationId`, `JobType`, `Payload` (jsonb), `Status` (Queued/Running/Completed/Failed), `Priority`, `RetryCount`, `MaxRetries`, `ResultReference` (nullable, format: `EntityType:EntityId`), `ErrorMessage`, `ScheduledAt`, `StartedAt`, `CompletedAt`, `CronExpression`, `CreatedBy` (→ UserProfile)
- `JobLogs` — Per-attempt execution log
  - `Id`, `JobId`, `Attempt`, `Status` (Running/Completed/Failed), `Message`, `ErrorMessage`, `StartedAt`, `CompletedAt`

**Enums:**
- `JobStatus`: `Queued`, `Running`, `Completed`, `Failed`

---

### 7. Email Sending & Monitoring

**Requirements:**
- Email abstraction supporting SMTP and transactional providers (SendGrid, Mailgun)
- Template engine: Razor-based email templates stored in filesystem or database
- Template variables with strongly-typed models
- Email queue: all emails go through the job worker, not sent synchronously
- Email log: every sent email is recorded with status (Queued, Sent, Failed, Bounced, Opened)
- Bounce and complaint webhook handling (provider-specific)
- Admin UI: email log viewer with search/filter, template management, test send
- Rate limiting configurable at tenant level

**Database Tables:**
- `EmailTemplates` — Template definitions
  - `Id`, `Name`, `SubjectTemplate`, `BodyTemplate`, `Category`
- `EmailLog` — Sent email history
  - `Id`, `OrganizationId`, `TemplateId`, `ToAddress`, `Cc`, `Bcc`, `Subject`, `BodyPreview`, `Status`, `SentAt`, `ErrorMessage`, `ProviderMessageId`, `OpenedAt`

---

### 8. Notification System

In-app notifications for specific users, independent of email.

**Requirements:**
- Notification types: Info, Warning, Success, Error, Action Required
- Delivery channels: In-app (real-time via SignalR), Email, Push (future)
- Notification preferences per user per notification type (opt in/out per channel)
- Mark as read / dismiss / dismiss all
- Notification badge count (real-time update)
- Notification grouping (e.g., "5 new invoices approved" instead of 5 separate notifications)
- Notification templates with variable substitution
- Bulk notification support (notify all users in a role or organization)

**Database Tables:**
- `NotificationTemplates` — Template definitions
- `Notifications` — Notification instances
  - `Id`, `UserId`, `Type`, `Title`, `Message`, `LinkUrl`, `IsRead`, `ReadAt`, `CreateDate`, `GroupKey`
- `UserNotificationPreferences` — Per-user channel opt-in/out

**Real-Time:**
- SignalR hub for pushing notifications to connected clients
- Fallback polling for WASM clients if SignalR unavailable

---

### 9. Settings & Configuration Management

**Requirements:**
- Four-tier settings: System (global), Tenant, Organization, User
- Settings stored in database as key-value with type information
- Settings cached with invalidation on change
- Admin UI for managing system, tenant, and organization settings
- User preferences UI (timezone, language, notification prefs, dashboard layout)
- Settings grouped by category for organized display
- Change history on setting modifications (ties into audit log)
- Inheritance: Organization settings inherit from parent organization, overridable at each level

**Database Tables:**
- `Settings` — Unified settings table
  - `Id`, `TenantId` (null for system), `OrganizationId` (null for system/tenant), `UserId` (null for system/tenant/org), `Category`, `Key`, `Value` (jsonb), `ValueType`, `Description`, `IsSensitive`

---

### 10. Structured Logging & Health Checks

**Requirements:**
- DevInstance.LogScope for scope-based tracing and profiling in application code, integrated with Microsoft.Extensions.Logging
- Serilog as the logging pipeline with structured output
- Log sinks: Console (dev), PostgreSQL table (production), optional Seq/Elasticsearch
- Correlation ID middleware: every request gets a unique ID carried through all logs
- Health check endpoints: `/health` (basic), `/health/ready` (dependencies)
- Health checks for: Database connectivity, Background worker status, Email provider, File storage, External API dependencies
- Log levels configurable at runtime without restart
- Sensitive data masking in logs (passwords, tokens, PII)

**Database Tables:**
- `ApplicationLogs` — Structured log storage (if using DB sink)
  - `Id`, `Timestamp`, `Level`, `Message`, `Exception`, `Properties` (jsonb), `CorrelationId`, `UserId`, `OrganizationId`

---

### 11. Exception Handling & API Error Responses

**Requirements:**
- API controllers use `HandleWebRequestAsync()` from WebServiceToolkit, which provides standardized exception-to-HTTP-status mapping
- Exception types from WebServiceToolkit: `BadRequestException` (400), `UnauthorizedException` (401), `RecordNotFoundException` (404), `RecordConflictException` (409)
- Additional custom exception type: `BusinessRuleException` for domain-level validation failures (maps to 422 Unprocessable Entity)
- Blazor SSR pages use error page middleware for unhandled exceptions
- Validation errors return structured field-level error messages
- Error correlation ID returned to user for support reference
- Unhandled exceptions logged with full context via LogScope, sanitized response returned to client
- Developer exception page in Development environment only

---

## P2 — Extend Per Project

### 12. Import / Export Engine

**Requirements:**
- CSV and Excel import with configurable column mapping
- Validation pipeline: required fields, type checking, duplicate detection, custom rules
- Import preview: show first N rows with validation results before committing
- Error report: downloadable file with row-level errors
- Export to CSV and Excel with column selection and filtering
- Background processing for large files (ties into job worker)

---

### 13. API Key Management

**Requirements:**
- API keys for external system integrations
- Key generation, rotation, and revocation
- Scope-limited keys (specific permissions per key)
- Optionally scoped to specific organization(s)
- Usage tracking and rate limiting per key
- Admin UI for key management

**Database Tables:**
- `ApiKeys` — Key records
  - `Id`, `OrganizationId` (null for tenant-wide), `Name`, `KeyHash`, `Prefix` (for identification), `Scopes` (jsonb), `CreatedBy` (→ UserProfile), `ExpiresAt`, `LastUsedAt`, `IsRevoked`

---

### 14. Webhook Support

**Requirements:**
- Outbound: Register webhook URLs for specific events (e.g., `Invoice.Created`)
- Outbound: Retry with exponential backoff, signature verification (HMAC)
- Outbound: Delivery log with request/response details
- Inbound: Webhook receiver endpoints with signature validation per provider
- Admin UI: webhook management, delivery log viewer, manual retry

**Database Tables:**
- `WebhookSubscriptions` — Registered webhook endpoints
- `WebhookDeliveries` — Delivery attempt log

---

### 15. Feature Flags

**Requirements:**
- Toggle features globally, per organization, or per user
- Flag types: Boolean, Percentage rollout, User list
- Runtime evaluation without restart
- Admin UI for flag management
- Code integration via `IFeatureFlagService.IsEnabled("FeatureName")`

**Database Tables:**
- `FeatureFlags` — Flag definitions
  - `Id`, `Name`, `Description`, `IsEnabled`, `OrganizationId` (null for global), `RolloutPercentage`, `AllowedUsers` (jsonb)

---

### 16. Localization / i18n Scaffolding

**Requirements:**
- Resource-based localization using .resx or database-driven strings
- User locale preference stored in profile
- Date, time, number, and currency formatting per locale
- RTL support scaffolding in CSS/layout
- Admin UI for managing translation strings (if database-driven)

---

### 17. Dashboard / Widget Framework

**Requirements:**
- Pluggable dashboard with configurable widget layout per user
- Widget interface: `IDashboardWidget` with `Render()`, `GetData()`, `GetDefaultSize()`
- Built-in widgets: Welcome/Getting Started, Recent Activity, Quick Stats, Notifications
- Widget settings per user (size, position, collapsed state)
- Drag-and-drop layout (stored in user preferences)
- Widgets respect organization scoping (stats widget shows data for user's visible organizations)

---

### 18. Rate Limiting

**Requirements:**
- Rate limiting middleware for API endpoints
- Configurable per endpoint, per tenant, per API key
- Sliding window algorithm
- Rate limit headers in responses (`X-RateLimit-Limit`, `X-RateLimit-Remaining`)
- Override capability for specific API keys

---

## Database Migration Strategy

- Entity Framework Core Migrations as the primary migration tool
- Migrations stored in `DevCoreApp.Database/Migrations/`
- Naming convention: `YYYYMMDDHHMMSS_DescriptiveName`
- Seed data applied via `IDataSeeder` interface, run after migrations
- Environment-specific seed data (Development gets sample data, Production gets only system defaults)
- Rollback scripts maintained alongside forward migrations for critical tables

---

## Project Structure

The solution is organized by **Client/Server** split at the top level, with **feature folders** (vertical slices) inside each project. Each feature carries its own entities, queries, decorators, services, controllers, and pages together rather than separating by technical layer.

```
DevCoreApp/
├── src/
│   ├── Client/
│   │   ├── DevCoreApp.Client/                     # Blazor WASM app (field workers)
│   │   │   ├── Invoices/
│   │   │   │   ├── InvoiceListPage.razor
│   │   │   │   └── InvoiceDetailPage.razor
│   │   │   ├── Customers/
│   │   │   │   └── ...
│   │   │   └── Shared/                            # Shared layouts, components
│   │   │
│   │   └── DevCoreApp.Client.Services/            # Client-side service implementations
│   │       ├── Invoices/
│   │       │   └── InvoiceApiClient.cs
│   │       ├── Customers/
│   │       │   └── CustomerApiClient.cs
│   │       └── Auth/
│   │           └── AuthStateProvider.cs
│   │
│   ├── Server/
│   │   ├── Admin/
│   │   │   ├── DevCoreApp.Admin.Services/         # Business logic, auth, notifications
│   │   │   │   ├── Invoices/
│   │   │   │   │   ├── InvoiceService.cs
│   │   │   │   │   └── InvoiceValidator.cs
│   │   │   │   ├── Customers/
│   │   │   │   │   ├── CustomerService.cs
│   │   │   │   │   └── CustomerValidator.cs
│   │   │   │   ├── Auth/
│   │   │   │   │   ├── PermissionClaimsTransformation.cs
│   │   │   │   │   ├── PermissionPolicyProvider.cs
│   │   │   │   │   └── OrganizationContextResolver.cs
│   │   │   │   ├── Notifications/
│   │   │   │   │   ├── NotificationService.cs
│   │   │   │   │   └── NotificationHub.cs
│   │   │   │   └── Settings/
│   │   │   │       └── SettingsService.cs
│   │   │   │
│   │   │   └── DevCoreApp.Admin.WebService/       # Blazor SSR host + API controllers + SignalR hubs
│   │   │       ├── Invoices/
│   │   │       │   ├── InvoiceController.cs
│   │   │       │   ├── InvoiceListPage.razor
│   │   │       │   └── InvoiceDetailPage.razor
│   │   │       ├── Customers/
│   │   │       │   ├── CustomerController.cs
│   │   │       │   └── CustomerListPage.razor
│   │   │       ├── Admin/
│   │   │       │   ├── UsersPage.razor
│   │   │       │   ├── RolesPage.razor
│   │   │       │   ├── OrganizationTreePage.razor
│   │   │       │   ├── JobDashboardPage.razor
│   │   │       │   └── EmailLogPage.razor
│   │   │       ├── Program.cs
│   │   │       └── appsettings.json
│   │   │
│   │   ├── DevCoreApp.Worker/                     # Background job worker (separate hosted service)
│   │   │   ├── Handlers/
│   │   │   │   ├── SendEmailHandler.cs
│   │   │   │   ├── GenerateReportHandler.cs
│   │   │   │   └── DataSyncHandler.cs
│   │   │   ├── JobWorkerService.cs
│   │   │   ├── Program.cs
│   │   │   └── appsettings.json
│   │   │
│   │   ├── DevCoreApp.Database/                   # Entities, queries, decorators, EF config
│   │   │   ├── Core/
│   │   │   │   ├── Models/
│   │   │   │   │   └── Base/
│   │   │   │   │       ├── DatabaseBaseObject.cs      # Id (Guid PK)
│   │   │   │   │       ├── DatabaseObject.cs          # + PublicId, CreateDate, UpdateDate
│   │   │   │   │       └── DatabaseEntityObject.cs    # + CreatedBy, UpdatedBy (UserProfile)
│   │   │   │   ├── Data/
│   │   │   │   │   ├── IOperationContext.cs        # User, org, IP, correlation ID
│   │   │   │   │   └── Queries/
│   │   │   │   │       └── BasicsImplementation/   # Base IModelQuery implementations
│   │   │   │   └── Audit/
│   │   │   │       ├── AuditInterceptor.cs         # Depends on IOperationContext
│   │   │   │       ├── AuditLog.cs
│   │   │   │       ├── AuditExcludeAttribute.cs
│   │   │   │       └── AuditConfiguration.cs
│   │   │   ├── Invoices/
│   │   │   │   ├── Invoice.cs                      # Entity
│   │   │   │   ├── InvoiceConfiguration.cs         # EF config
│   │   │   │   ├── InvoiceQuery.cs                 # Query implementation
│   │   │   │   └── InvoiceDecorator.cs             # Entity ↔ ViewModel mapper
│   │   │   ├── Customers/
│   │   │   │   ├── Customer.cs
│   │   │   │   ├── CustomerConfiguration.cs
│   │   │   │   ├── CustomerQuery.cs
│   │   │   │   └── CustomerDecorator.cs
│   │   │   ├── Organizations/
│   │   │   │   ├── Organization.cs
│   │   │   │   ├── Tenant.cs
│   │   │   │   ├── UserOrganization.cs
│   │   │   │   ├── OrganizationConfiguration.cs
│   │   │   │   ├── OrganizationQuery.cs
│   │   │   │   └── OrganizationDecorator.cs
│   │   │   ├── Identity/
│   │   │   │   ├── ApplicationUser.cs
│   │   │   │   ├── ApplicationRole.cs
│   │   │   │   └── IdentityConfiguration.cs
│   │   │   ├── Permissions/
│   │   │   │   ├── Permission.cs
│   │   │   │   ├── RolePermission.cs
│   │   │   │   ├── UserPermissionOverride.cs
│   │   │   │   └── PermissionConfiguration.cs
│   │   │   ├── Jobs/
│   │   │   │   ├── Job.cs
│   │   │   │   ├── JobLog.cs
│   │   │   │   ├── JobConfiguration.cs
│   │   │   │   ├── JobQuery.cs
│   │   │   │   └── JobDecorator.cs
│   │   │   ├── Email/
│   │   │   │   ├── EmailLog.cs
│   │   │   │   ├── EmailTemplate.cs
│   │   │   │   ├── EmailConfiguration.cs
│   │   │   │   ├── CoreEmailLogQuery.cs
│   │   │   │   └── EmailLogDecorator.cs
│   │   │   ├── Files/
│   │   │   │   ├── FileRecord.cs
│   │   │   │   ├── FileConfiguration.cs
│   │   │   │   ├── FileQuery.cs
│   │   │   │   └── FileDecorator.cs
│   │   │   ├── Notifications/
│   │   │   │   ├── Notification.cs
│   │   │   │   ├── NotificationTemplate.cs
│   │   │   │   ├── UserNotificationPreference.cs
│   │   │   │   ├── NotificationConfiguration.cs
│   │   │   │   ├── NotificationQuery.cs
│   │   │   │   └── NotificationDecorator.cs
│   │   │   ├── Settings/
│   │   │   │   ├── Setting.cs
│   │   │   │   ├── SettingConfiguration.cs
│   │   │   │   └── SettingQuery.cs
│   │   │   ├── Migrations/
│   │   │   └── ApplicationDbContext.cs
│   │   │
│   │   ├── DevCoreApp.Email/                      # Email provider abstractions & implementations
│   │   └── DevCoreApp.Storage/                    # File storage provider abstractions & implementations
│   │
│   └── DevCoreApp.Shared/                        # ViewModels, constants, enums (cross Client & Server)
│       ├── Invoices/
│       │   ├── InvoiceViewModel.cs
│       │   └── InvoiceCreateRequest.cs
│       ├── Customers/
│       │   ├── CustomerViewModel.cs
│       │   └── CustomerCreateRequest.cs
│       ├── Email/
│       │   └── EmailLogViewModel.cs
│       ├── Jobs/
│       │   └── JobViewModel.cs
│       ├── Notifications/
│       │   └── NotificationViewModel.cs
│       └── Common/
│           ├── Enums.cs
│           └── Constants.cs
│
├── tests/                                         # Mirrors src/ structure
│   ├── Client/
│   │   └── DevCoreApp.Client.Tests/
│   ├── Server/
│   │   ├── DevCoreApp.Admin.Services.Tests/
│   │   ├── DevCoreApp.Admin.WebService.Tests/
│   │   ├── DevCoreApp.Worker.Tests/
│   │   ├── DevCoreApp.Database.Tests/
│   │   ├── DevCoreApp.Email.Tests/
│   │   └── DevCoreApp.Storage.Tests/
│   └── DevCoreApp.Integration.Tests/
│
├── docs/
│   ├── Architecture.md
│   ├── Permissions.md
│   ├── OrganizationHierarchy.md
│   └── Deployment.md
├── scripts/
│   ├── SeedDevData.sql
│   └── SetupLocal.sh
└── DevCoreApp.sln
```

---

## Data Access Patterns

### Query Abstraction

All database access is abstracted behind query classes. Services never call `DbContext` directly. Each feature has its own query class (e.g., `InvoiceQuery`, `CoreEmailLogQuery`) that implements `IModelQuery<T,D>` from WebServiceToolkit.Database. The query interfaces provide a composable, fluent API:

- `IModelQuery<T,D>` — Base query interface with CRUD operations
- `IQPageable<T>` — Skip/Take pagination (`.Top()`, `.Page()`)
- `IQSearchable<T,K>` — Search and lookup by public ID
- `IQSortable<T>` — Sort by column

Query classes encapsulate all LINQ/SQL for their feature, making data access testable and swappable. Cross-feature queries (e.g., "overdue invoices with customer details and attached files") live in the feature that owns the primary entity. The query class can reference any entity — the query is the unit of organization, not the table.

### Decorators (Entity ↔ ViewModel Mappers)

Entities never cross the server boundary. Every entity that the client displays has a corresponding ViewModel in `DevCoreApp.Shared` (typically extending `ModelItem` from WebServiceToolkit.Common for the `Id` property). Paginated collections use `ModelList<T>` from WebServiceToolkit.Common. Decorator classes in `DevCoreApp.Database` handle conversion in both directions:

```
Entity (Database)  ←→  Decorator  ←→  ViewModel (Shared)
```

The data flow for any feature:

```
Client (WASM)
  → calls API with InvoiceCreateRequest (ViewModel from Shared)
    → InvoiceController (WebService)
      → InvoiceService (Admin.Services)
        → InvoiceQuery (Database) — reads/writes via IModelQuery<T,D>
        → InvoiceDecorator (Database) — converts Entity ↔ ViewModel
      ← returns InvoiceViewModel (from Shared)
    ← JSON response
  ← renders in Blazor
```

### IOperationContext

The `IOperationContext` interface provides contextual information to the data layer without creating dependencies on ASP.NET Core. It is defined in `DevCoreApp.Database` and implemented differently per host:

```csharp
public interface IOperationContext
{
    Guid? UserId { get; }
    Guid? PrimaryOrganizationId { get; }
    IReadOnlySet<Guid> VisibleOrganizationIds { get; }
    string? IpAddress { get; }
    string? CorrelationId { get; }
}
```

| Host | Implementation |
|------|---------------|
| `Admin.WebService` | Populated from `HttpContext`, `ClaimsPrincipal`, and resolved organization context |
| `Worker` | Populated from the job's stored context (user who queued the job, their organization) |
| Database triggers | No context available — `ChangedByUserId` is null (expected) |

The `AuditInterceptor`, organization scoping filter, and `IQueryRepository` all depend on `IOperationContext` rather than `IHttpContextAccessor`. This keeps `DevCoreApp.Database` free of ASP.NET Core dependencies.

---

## Dependency Flow

```
DevCoreApp.Shared                  ← ViewModels, enums, constants (referenced by everyone)
DevCoreApp.Database                ← Entities, queries, decorators, EF config
                                      References: Shared (for ViewModels in decorators)
DevCoreApp.Email                   ← Email provider abstractions & implementations
                                      References: Shared
DevCoreApp.Storage                 ← File storage provider abstractions & implementations
                                      References: Shared
DevCoreApp.Admin.Services          ← Business logic, auth, notifications
                                      References: Database, Email, Storage, Shared
DevCoreApp.Admin.WebService        ← Web host, controllers, Blazor SSR pages, SignalR hubs
                                      References: Admin.Services, Shared
DevCoreApp.Worker                  ← Background job worker (separate hosted process)
                                      References: Admin.Services, Database, Shared
DevCoreApp.Client.Services         ← Client-side API clients
                                      References: Shared
DevCoreApp.Client                  ← Blazor WASM app
                                      References: Client.Services, Shared
```

Key constraint: `DevCoreApp.Client` and `DevCoreApp.Client.Services` never reference `DevCoreApp.Database` or any server project. The only shared surface is `DevCoreApp.Shared` (ViewModels and enums).

---

## Implementation Order

This is the recommended order of implementation to minimize rework and maximize early usability.

| Phase | Features | Rationale |
|-------|----------|-----------|
| **Phase 1** | Solution structure, `DevCoreApp.Database` setup (ApplicationDbContext, IOperationContext, base query implementations), Tenant + Organization hierarchy, organization scoping filter, Audit logging interceptor | Foundation everything else builds on |
| **Phase 2** | ApplicationUser/ApplicationRole (Identity), UserOrganizations, Roles & Permissions, ClaimsTransformation, Organization context resolution, Authentication (Cookie + JWT) | Can't build anything user-facing without auth and scoping |
| **Phase 3** | Settings management (4-tier), Structured logging, Exception handling, Health checks | Operational baseline for a running app |
| **Phase 4** | `DevCoreApp.Worker` setup, Background job system (Jobs/JobLogs), Email system (queue + templates + sending via `DevCoreApp.Email`) | Enables async processing and user communication |
| **Phase 5** | Notification system (SignalR + in-app), File storage abstraction (`DevCoreApp.Storage`) | Completes the core interactive experience |
| **Phase 6** | Admin UI pages for all of the above (including Organization management tree, Job dashboard, Email log viewer) | Management interface for all P0/P1 features |
| **Phase 7** | Import/Export, API keys, Webhooks, Feature flags | Per-project features, implement as needed |

---

## Codebase Conventions

- **Business logic lives in `Admin.Services`, not in `Database`.** Entity classes in `Database` are data models only. If an entity method does more than simple property computation, it belongs in a service.
- **All database access goes through query classes.** Services never call `DbContext` directly. Query classes implement `IModelQuery<T,D>` from WebServiceToolkit.Database and are the single point of data access per feature.
- **Entities never leave the server.** Every entity displayed by the client has a corresponding ViewModel in `Shared` (extending `ModelItem` where appropriate). Paginated results use `ModelList<T>`. Decorator classes handle the conversion. No exceptions.
- **Cross-feature queries live in the feature that owns the primary entity.** An overdue invoice report lives in `Invoices/`, not in a separate `Reporting/` folder.
- **`IOperationContext` is the bridge between the application layer and the data layer.** It replaces direct ASP.NET Core dependencies in `Database`.
- **Feature folders are the unit of organization.** Each feature folder contains everything related to that feature at that layer: entity, configuration, query, decorator (in Database); service, validator (in Admin.Services); controller, pages (in WebService); ViewModel, requests (in Shared).
- **Service registration uses toolkit attributes.** Server services use `[WebService]` (registered via `AddServerWebServices()`). Client services use `[BlazorService]` (registered via `AddBlazorServices()`). Manual DI registration is the exception.
- **API controllers use `HandleWebRequestAsync()`.** All controller actions wrap their logic with WebServiceToolkit's `HandleWebRequestAsync()` for standardized exception-to-HTTP-status mapping.
- **Query parameters use `[QueryModel]`.** API endpoints that accept filters, pagination, or sorting bind to POCO classes decorated with `[QueryModel]` from WebServiceToolkit.

---

## Notes

- **Entity base classes:**
  - `DatabaseBaseObject` — `Id` (Guid PK) only. Used for infrastructure tables that don't need public exposure (audit logs, job logs, settings).
  - `DatabaseObject` : `DatabaseBaseObject` — adds `PublicId` (string, client-facing ID), `CreateDate`, `UpdateDate`. Used for all entities exposed via API.
  - `DatabaseEntityObject` : `DatabaseObject` — adds `CreatedBy` / `UpdatedBy` (navigation properties to `UserProfile`). Used for entities that track who created/modified them.
- The internal `Guid Id` primary key is never exposed to the client. The API uses `PublicId` exclusively. Decorators map `PublicId` → `ModelItem.Id` on ViewModels.
- `PublicId` values are generated via `DevInstance.DevCoreApp.Shared.Utils.IdGenerator.New()`
- All business tables include `OrganizationId` with index and global query filter
- `Tenant` is deployment-level metadata only — not used on business tables
- Soft delete via `IsDeleted` / `DeletedAt` on entities where needed (configurable per entity)
- All timestamps stored as UTC, converted to user timezone in UI layer
- API versioning via URL path (`/api/v1/`) from the start
- ASP.NET Identity tables (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc.) retain their default names for compatibility, but the entity classes are extended with PascalCase custom properties
- `DevCoreApp.Worker` runs as a separate hosted process from `DevCoreApp.Admin.WebService`, enabling independent deployment, scaling, and restarts
