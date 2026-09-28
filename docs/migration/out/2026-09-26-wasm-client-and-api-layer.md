---
origin: DevCoreApp
targets: [ThreadIQ, Tentrie]
scope:
  # Renamed projects (sync keys change: Server.Admin.* → Server.Api.* / Server.Services.*)
  - Server.Api.*                                   # was Server.Admin.WebService.*
  - Server.Services.*                              # was Server.Admin.Services.*
  - Server.Services.Mocks.*                        # was Server.Admin.Services.Mocks.*
  # API layer (new / rewritten)
  - Server.Api.Core.Controllers.*                  # ApiControllerBase, ApiConfigurationExtensions, 16 controllers
  - Server.Api.Core.Middleware.ApiExceptionHandler
  - Server.Api.Core.Middleware.ApiErrorResponse    # moved from Shared.Model.Core
  - Server.Api.Core.Hosting.WasmClientHosting
  - Server.Api.Core.Pages.*                        # Razor Pages: Setup, Error
  - Server.Api.Program
  - Shared.Utils.Core.Json.UtcDateTimeJsonConverter
  - Shared.Model.Core.Common.{PagedList,ListQuery}  # + feature queries (AuditLogQuery, JobQuery, EmailLogQuery, OrganizationQuery, SettingQuery)
  - Shared.Model.Core.Permissions.PermissionClaims
  - Shared.Model.Core.UserAdmin.{CurrentUserItem,UserRequests}
  - Shared.Model.Core.Account.{ConfirmEmailRequest,InvitationPasswordRequest}
  - Shared.Model.Core.ImportExport.ImportCommitRequest
  # Services
  - Server.Services.Core.AccountService / IAccountService         # rewritten: API flows + owner setup, no cookie sign-in
  - Server.Services.Core.GridProfileService / IGridProfileService
  - Server.Services.Core.UserAdmin.{CurrentUserService,ICurrentUserService}
  - Server.Services.Core.Notifications.{INotificationService,NotificationService,NotificationSettings}
  - Server.Services.Core.Exceptions.BusinessRuleException
  # Clients
  - Client.Services.Core.*                         # Api, Auth, Time, Me, Users, Notifications + one folder per admin feature
  - Client.Desktop.*                               # new project: the admin UI
  - Client.Mobile.*                                # was Client.* (renamed)
  - Client.Services.Mocks.*                        # new project
  # Removed
  - Server.Admin.WebService.Core.UI.*              # Blazor Server UI — deleted
  - Server.Admin.WebService.Core.Identity.{IdentityComponentsEndpointRouteBuilderExtensions,IdentityRedirectManager,IdentityRevalidatingAuthenticationStateProvider,IdentityNoOpEmailSender}
status: pending
related:
  - docs/WasmMigrationPlan.md                      # full plan, decisions D1–D8, per-phase findings
  - "DevCoreApp commits f16b64d, c92224a, 575a317, d03a3da, 9130404, 1bb1644 (branch migration/wasm, #1179)"
  - "WebServiceToolkit 10.3.0 (WebServiceError contract), 10.3.2 ([QueryModel] string binding fix)"
  - "BlazorToolkit 10.4.0 (ServiceActionErrorType renumbered), 10.5.0 (IModelList pager, CacheableSource decoupled from ModelList)"
---

# Move the UI to Blazor WebAssembly over an `/api` layer, and rename `Server.Admin.*`

## Why

Blazor interactive Server stopped fitting: a live circuit per user, a server-side `DbContext` shared
across concurrent components, and no path to an offline or mobile client. DevCoreApp now has:

- a **server that only serves HTTP**: `/api` (thin controllers, one service call each), the SignalR
  hub, health checks, and one server-rendered page, the first-run owner `/setup`;
- **two Blazor WebAssembly clients**: `Client.Desktop` (the admin UI, at `/`) and `Client.Mobile`
  (at `/mobile`). Both are hosted by the server and share one `Client.Services` layer;
- **UTC everywhere on the wire**: clients convert for display;
- the server projects renamed from `Server.Admin.{WebService,Services}` to
  `Server.{Api,Services}`, because nothing in them is "admin" any more.

This is shared `Core` in every repo, and both forks already run a WASM client against their own
`/api`, so the value is in converging on one contract. The divergence is real: ThreadIQ and
Tentrie each wrap responses differently, and ThreadIQ sends local times.

This is one large doc on purpose. The rename changes every sync key, so doing it separately
would mean touching every file twice.

## Current state

| | DevCoreApp (after) | ThreadIQ (now) | Tentrie (now) |
|---|---|---|---|
| Core/App restructure | applied | applied | **not applied** — pages in `UI/Pages`, services in feature folders without `Core/` |
| Admin UI | `Client.Desktop` (WASM) | Blazor Server + product (CRM) pages | Blazor Server, ~26 product pages (CBA, Master, Operations, Dashboard, Workflows) |
| Field client | `Client.Mobile` (at `/mobile`) | `ThreadIQ.Client` PWA at `/mobile` (publish script) | `Tentrie.Client` PWA (JWT, own `AuthService`/`AuthTokenHandler`/`CacheableSource`) |
| API response shape | bare result, `WebServiceError` on failure | CRM: `ServiceActionResult` envelope; Core: bare | 89 actions return the envelope, 10 bare |
| Dates on the wire | UTC (`…Z`) | **user-local** (decorators call `ToLocal(UserTimeZone)`) | UTC |
| API authorization | permission policy per verb | bare `[Authorize]` | per controller (check) |
| WebServiceToolkit / BlazorToolkit | 10.3.2 / 10.5.0 | ? / 10.4.0 | 10.1.0 / 10.1.2 |

## Prerequisites

1. **Tentrie:** apply the Core/App restructure first, in order:
   `2026-07-23-adopt-core-app-structure.md`, `2026-08-06-core-app-restructure.md` and
   `2026-08-28-app-marker-collision-and-core-helpers.md`. Every sync key below assumes that layout.
2. **Packages**, all repos:
   - WebServiceToolkit **10.3.2**;
   - BlazorToolkit **10.5.0**;
   - `Microsoft.*` **10.0.12**, which BlazorToolkit 10.5.0 requires.

   ⚠ BlazorToolkit 10.4.0 inserted `Exception = 1` into `ServiceActionErrorType`, and the server's
   `WebServiceErrorType` uses that numbering. **Server and every client must move together.** A
   client on ≤ 10.3 misreads every error type. That includes a shipped mobile build: Tentrie.Client
   is on 10.1.2.

## Proposed change

Apply in this order. After each step the fork should build and run, and the old UI keeps working
until step 9.

### 0. Rename the server projects

| Before | After (folder / project / root namespace) |
|---|---|
| `src/Server/Admin/WebService` / `DevCoreApp.Admin.WebService` | `src/Server/Api` / `DevCoreApp.Server.Api` / `DevInstance.<Product>.Server.Api` |
| `src/Server/Admin/Services` / `DevCoreApp.Admin.Services` | `src/Server/Services` / `DevCoreApp.Server.Services` / `DevInstance.<Product>.Server.Services` |
| `mocks/Server/Admin/ServicesMocks` | `mocks/Server/Services.Mocks` / `…Server.Services.Mocks` |
| `tests/Server/WebService/WebService.Tests.csproj` | `tests/Server/Api/Api.Tests.csproj` (assembly unchanged) |

- **Namespaces.** Replace, longest first:
  1. `…Server.Admin.Services.Mocks` → `…Server.Services.Mocks`
  2. `…Server.Admin.Services` → `…Server.Services`
  3. `…Server.Admin.WebService` → `…Server.Api`

  This includes `App` code.
- **Relative `ProjectReference`s** out of the moved projects lose one `..\` (the `Admin` level is
  gone). Verify that every reference resolves.
- **Check** with the Rule 2 probe (a throwaway `namespace <Root>.App; internal class Probe {}` in each
  project) and a grep for `Server.Admin`.

### 1. Server fixes that stand alone (land them first)

Pre-existing bugs, each independent of the WASM work:

- **`ApiExceptionHandler` never matched.** With `UseExceptionHandler("/Error")`, .NET 10
  rewrites `Request.Path` to `/Error` before `IExceptionHandler`s run. Read
  `IExceptionHandlerPathFeature.Path` instead, and set `AllowStatusCode404Response = true`,
  or a 404 from the handler is rethrown.
- **SignalR WebSockets never authenticated.** The Smart scheme selector sent `/hubs` requests
  carrying `?access_token=` to the cookie scheme, so every WebSocket got a 302 and fell back to
  long polling. Route `/hubs` + `access_token` to JwtBearer, and answer 401 (not the login
  redirect) for `/hubs` in `OnRedirectToLogin`.
- **Audit trigger timestamps.** `audit_trigger_function()` stored `NOW() AT TIME ZONE 'UTC'` into
  a `timestamptz` column, which shifts every database-sourced row by the session's UTC offset.
  - Take the fixed `AuditTriggerExtensions` (`NOW()`).
  - Add an empty migration whose `Up` calls `migrationBuilder.CreateAuditTriggerFunction();`.
    EF scaffolds it with an empty `Up`, so the call must be added by hand.
  - Existing rows stay shifted.
- **Ownership checks the API made reachable:**
  - `NotificationService.MarkAsReadAsync` must check that the notification is the caller's;
  - profile-picture upload/delete must be self or `Admin.Users.Edit`;
  - `UpdateCurrentUserAsync` must not let a user change their own email;
  - `GridProfileService` must throw `ForbiddenException`, not `UnauthorizedException` (a 401
    makes JWT clients refresh and retry).
- Identity cookie: `HttpOnly = true`.

### 2. Wire contract

- **`ApiControllerBase`** (`Server.Api.Core.Controllers`): `HandleServiceAsync(() => service.X(...))`
  returns the **bare result**. A failed `ServiceActionResult` becomes an exception: not authorized →
  403, validation → 400 naming the property, general → 422, anything else → 500. Every controller
  derives from it and makes **one** service call per action.
- **`AddApiControllers()`** (`ApiConfigurationExtensions`), used by `Program` and by the tests:
  - `UtcDateTimeJsonConverter`;
  - `.AddWebServiceToolkitQuery()` and `.AddWebServiceToolkitErrors()`;
  - removes `HttpNoContentOutputFormatter`, so `null` is JSON `null` with a 200, not an empty 204.
- **`ApiExceptionHandler`** uses `ControllerUtils.ToWebServiceError` and adds `CorrelationId`
  (`ApiErrorResponse : WebServiceError`, now in `Server.Api.Core.Middleware`).
- **`BusinessRuleException`** derives from `UnprocessableEntityException`, so it returns 422
  everywhere.
- **Tests:** copy `tests/Server/Api/Core/Api/ApiContractTests.cs`. It pins the contract through a
  real pipeline and reads errors as BlazorToolkit's `ServiceActionError`.

### 3. List types and query models

- `ModelList<T>` / `ModelItem` are obsolete. Use `Shared.Model.Core.Common.PagedList<T>`, an
  `IModelList<T>` with the same JSON, plus `PagedList.Create(...)`, and have DTOs implement
  `IModelItem` with their own `Id`.
- **`ListQuery`** (`[QueryModel]`): `top`, `page`, `sortBy=-Field,Other`, `search`. There is no
  `+` prefix, because it decodes to a space. `DateRangeListQuery` normalizes dates to UTC, and
  feature queries derive from it.

### 4. Services

- **`IAccountService`** replaces the cookie-era `AccountService`:
  - register, forgot/reset password, confirm email, invitation set-password;
  - `IsSetupRequiredAsync`, `SetupOwnerAsync`;
  - failures throw 400;
  - email links are built by `IAccountLinkBuilder` against the client routes in `AccountRoutes`.
  - **The anonymous set-password re-verifies the emailed token.** Without that, anyone can set the
    first password of any invited account.
- **`ICurrentUserService`** → `CurrentUserItem` (profile, roles, effective permissions, theme,
  `RealTimeNotifications`) for `GET api/me`.
- **`IGridProfileService`** is extracted from the concrete class.
- **Notifications:** current-user methods (`GetMyNotificationsAsync`, `GetMyUnreadCountAsync`,
  `MarkAllMyReadAsync`).
- **`RequiresOrganizationSelection`** returns `ServiceActionResult<bool>`.
- ⚠ `AddBlazorServices` registers a class **only under its interfaces** once it has any. Inject by
  interface.

### 5. Controllers

These go in `Server.Api.Core.Controllers`, with a permission per verb (`GET` = View, `POST` =
Create, `PUT` = Edit, `DELETE` = Delete/Revoke):
- `api/account`, `api/me`, `api/users`, `api/roles`, `api/permissions`, `api/organizations`;
- `api/email-logs`, `api/jobs`, `api/audit-logs`;
- `api/feature-flags`, `api/api-keys`, `api/webhooks`, `api/settings`;
- `api/notifications`, `api/grid-profiles`, `api/import-export`.

`api/auth`, `api/files` and the profile-picture controller move onto `ApiControllerBase`.
`api/user/profile` is removed in favour of `api/me/profile`.

### 6. Real-time notifications are optional

`Notifications:RealTime` (`NotificationSettings`, default `true`) decides whether the hub is
mapped at all. Clients use `IUnreadNotificationsMonitor`: REST first, the hub only when
offered, polling every 60 s while not connected, and a re-sync after a reconnect. **Several
instances without a SignalR backplane (Redis / Azure SignalR) must set it to `false`.**

### 7. Shared client layer: `Client.Services.Core`

- `Api/`: `ApiServiceBase` (BlazorToolkit `IApiContext` → `ServiceActionResult`) and
  `ApiQueryExtensions`. BlazorToolkit's URL builder neither escapes values nor formats them
  invariantly, so **always** use `.Query(model)`.
- `Auth/`:
  - `AuthTokenStore` (a singleton persisted to `localStorage`);
  - `TokenRefresher` (single-flight, because the server treats reuse of a rotated refresh token as
    theft);
  - `AuthTokenHandler` (refresh near expiry, refresh once on 401 and replay);
  - `ApiAuthenticationStateProvider` (claims from `api/me`) and `ClientPermissionPolicyProvider`.
- `Time/ILocalTimeService`: the profile zone, else the browser's. Display is `<LocalTime>` /
  `LocalClock`, and inputs are converted with `ToUtc` **in the client service**.
- `Me/`, `Users/IProfilePictureService` (a token fetch returned as a `data:` URL, because
  `<img src>` sends no token), and `Notifications/`.
- **One client service per admin feature**, with interfaces mirroring the server services, so pages
  port by swapping `using`s.
- `AddDevCoreClientServices(apiBase)` registers all of it.

### 8. Clients

- **`Client.Desktop`** (new):
  - the admin UI ported page for page, plus the account pages;
  - SCSS/TS moved from the server, and the `DartSassBuilder` copy target now hooks
    `DartSass_Build` (the old `DartSassCompile` target does not exist);
  - `-c ServiceMocks` runs with no server.
- **`Client.Mobile`** (renamed from `Client`):
  - served under `/mobile` (`StaticWebAssetBasePath`, `<base href="/mobile/">`);
  - login and profile over the same services.
- **`Client.Services.Mocks`**: the server mocks retargeted to the client interfaces, plus
  auth/me/grid/notifications/pictures/account mocks.
- **Standalone client dev:** `wwwroot/appsettings.Development.json` `DevServers` maps each dev-server
  origin to the Api host (`ApiBaseAddress.Resolve`); any other origin calls itself (hosted).
  ⚠ Do not switch on the environment: the .NET 10 WASM SDK bakes it in at build time
  (`WasmApplicationEnvironmentName`, `Development` for every Debug build) and ignores the launch
  profile's `ASPNETCORE_ENVIRONMENT`, and the Api host serves that same build.
- **Visual Studio:** `<Solution>.slnLaunch` (checked in) holds multi-project profiles that start the
  Api (browserless `http-api`/`https-api` profiles) together with each client's dev server. A client
  with a base path (Mobile at `/mobile/`) needs `"pathbase": "/mobile"` in its launch profile's
  `environmentVariables`. The dev server ignores `StaticWebAssetBasePath` and would otherwise serve
  at `/`, so the app's `/mobile/_framework/…` requests 404.

### 9. Cut-over

- **Delete** `Server.Api.Core.UI/**`, `UI/App.razor`, `UI/Routes.razor`, `_Imports.razor`, the
  Identity Razor components and endpoints, the server's SCSS/TS, and the Blazor Components
  packages (keep `WebAssembly.Server`).
- **Add Razor Pages** (`RootDirectory = /Core/Pages`): `/setup` (anonymous, 404 once any user exists,
  no sign-in) and `/Error`.
- **Smart scheme:** the fallback is **JwtBearer**, not the Identity cookie.
- **Hosting:** the Api project **references** both client projects. Serve them with
  `app.MapStaticAssets()` plus `MapWasmClients()` (index.html fallbacks; `/api`, `/hubs` and
  `/health` stay 404).
  - ⚠ Do **not** add `UseBlazorFrameworkFiles`. It branches the pipeline for `/_framework` and
    those requests fail with a 500 ("reached the end of the pipeline without executing the
    endpoint").

## Per-fork notes

### ThreadIQ

- **Dates (breaking for the shipped mobile app).**
  - Server decorators must stop converting to `UserTimeZone`, and `ToRecord(..., UserTimeZone)`
    must stop converting back.
  - `ThreadIQ.Client` must convert with `ILocalTimeService`: `CrmDisplay.Relative` compares
    against `DateTime.Now` assuming local input.
  - **Ship server and mobile in one release**, or old mobile builds show UTC as local.
- **Envelope vs bare.**
  - `CrmCrudControllerBase` returns the full `ServiceActionResult` for BlazorToolkit's
    `CacheableSource`. BlazorToolkit 10.5 decoupled `CacheableSource` from `ModelList`.
  - Confirm it reads the bare list, then move `CrmCrudControllerBase` onto `ApiControllerBase`
    (`HandleServiceAsync`).
  - Until then, keep the envelope in a fenced `App` deviation. Do not put it in `Core`.
- **Authorization:** replace bare `[Authorize]` with permission policies. Product keys go in
  the `App` part of `PermissionDefinitions`.
- **Hosting:** `App/Hosting/MobileClientHosting` and `ec2/publish-mobile-client.ps1` are superseded
  by project references plus `WasmClientHosting` (Mobile at `/mobile` already matches).
- **Client auth:** `ThreadIQ.Client` has its own `AuthService`/`AuthTokenHandler`/`AuthTokenStore`
  (tokens in IndexedDB via `IObjectStore`). Replace it with `Client.Services.Core.Auth`, keeping an
  `ITokenStorage` implementation over `IObjectStore` in `App` if IndexedDB matters for offline.
  Logout should now call `api/auth/revoke`, which `IAuthService.LogoutAsync` does.
- **Product admin pages** (CRM on Blazor Server) must be ported to `Client.Desktop/App/UI/Pages`
  with client services in `Client.Services/App/<Feature>`, following the Core pages.

### Tentrie

- **Prerequisite:** the Core/App restructure (see Prerequisites).
- **`Tentrie.Client` is the field PWA**, i.e. the `Client.Mobile` role.
  - Keep it as the product's mobile client (rename or not), moving its product code under `App/`.
  - Adopt `Client.Services.Core` (`Api`, `Auth`, `Time`, `Me`, `Notifications`) in place of its own
    `Auth/*` and `Net/*`.
  - Its `Sync/` (CacheableSource, MasterDataSync) is product code: `App`.
- **Envelope → bare.** 89 controller actions return `Ok(result)` (the envelope), and
  `Tentrie.Client` unwraps it. Move the controllers to `ApiControllerBase` and update the client
  services in the **same** change.
- **Packages:** on WebServiceToolkit 10.1.0 / BlazorToolkit 10.1.2. See the error-type renumbering
  warning above; the server and `Tentrie.Client` must upgrade together.
- **Product admin pages:** ~26 of them must be ported to Desktop `App/UI/Pages`:
  - CBA: rate tables, rules, union locals, holidays;
  - Master: customers, employees, equipment, jobs, yards;
  - Operations: tickets, approval queue;
  - Dashboard;
  - Workflows (designer, stage editor).
- **Dates:** decorators do not convert to a user zone (only `BaseService.UserTimeZone` exists), so
  the UTC rule should hold. Grep for `ToLocal(` before assuming so.

## Affected shared surface

See `scope` above. Collisions to expect with local fences:

- `Program.cs` (both forks): hosting, auth scheme, and the removal of Razor components.
  ThreadIQ's `UseMobileClient`/`MapMobileClient` calls go.
- `AccountService`: rewritten. A fork with fenced account logic must re-apply it to the
  `IAccountService` shape.
- `NotificationService`, `UserProfileService` (ownership checks), `GridProfileService`, and
  `PermissionClaimsTransformation` (it now uses `PermissionClaims.Type`).

## Verification

1. `dotnet build` in Debug **and** `-c ServiceMocks`; all tests, including `ApiContractTests`,
   `Client.Services.Tests` and `UnreadNotificationsMonitorTests`.
2. Run **only** the Api host:
   - `/` serves Desktop and `/mobile/` serves Mobile, and both `_framework/blazor.webassembly.js`
     return 200;
   - `/setup` returns 404 when users exist; `/api/nope` returns 404, not HTML.
3. Sign in on Desktop and open every admin page:
   - every `/api` call is 2xx in the server log, and there are no `OPTIONS` preflights (same origin);
   - the unread count is fetched once and the hub negotiates.
4. Mobile at `/mobile/` shares the session. With `Notifications__RealTime=false` the hub
   returns 404 and the badge polls.
5. On an empty database, `/setup` creates an owner who can sign in as Owner with all permissions.
6. Audit: a trigger-written `AuditLogs` row's `ChangedAt` equals `now()`.

## Open questions

- **ThreadIQ:** does BlazorToolkit 10.5 `CacheableSource` read a bare `IModelList` payload? If
  not, it needs a toolkit change before CRM can drop the envelope.
- **Token storage for offline clients:** `localStorage` in Core, or `ITokenStorage` over IndexedDB
  as an `App` override? DevCoreApp has no offline requirement, so it keeps `localStorage`.
- **Known Core bug, not fixed in this change:** `UserProfileService.GetListAsync` pages profiles and
  then drops those whose Identity user is gone, so counts and pages are wrong when orphaned
  profiles exist. A follow-up doc will carry the fix.
