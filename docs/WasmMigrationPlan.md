# Plan — Migrate the Admin UI from Blazor Server to WASM + `/api`

Status: **approved 2026-09-25** (decisions below confirmed) · Branch: `migration/wasm` · 2026-09-25

## 1. Goal

- The server becomes an **API host**: everything under `/api/**`, plus SignalR (`/hubs/**`), health,
  and exactly **one server-rendered page** — the initial owner setup, rewritten as a plain Razor Page.
- All Admin UI moves to a new standalone Blazor WASM app **`DevCoreApp.Client.Desktop`**. The
  existing `DevCoreApp.Client` is renamed **`DevCoreApp.Client.Mobile`**.
- Controllers are thin: one service call per action, WebServiceToolkit `HandleWebRequestAsync`,
  common error handling in one place, no mapping/validation in the controller (any exception
  carries a comment explaining why).
- Client pages hold no business logic — they call client service classes (`[BlazorService]`,
  `IApiContext<T>`, `IServiceExecutionHost`) from BlazorToolkit.
- **All date/time values cross the wire as UTC.** Only the client converts to local, and only for
  display (and back to UTC on input).
- A migration instruction doc for ThreadIQ, Tentrie and future forks.

## 2. Findings that shape the plan

### 2.1 ThreadIQ (what you asked me to check)

ThreadIQ does expose an `/api` layer for its mobile app, but it is **only partly the pattern we want**:

| Aspect | ThreadIQ today | Take it? |
|---|---|---|
| Routes | Literal `[Route("api/...")]`, no versioning, no global prefix | **Yes** |
| Base controller | `CrmCrudControllerBase<TItem>` over `ICRUDService<T>` (list/get/add/update/delete); subclasses only override `Service` | **Yes** — generalize into `Core` |
| Controller body | `HandleWebRequestAsync`, one service call (two small exceptions: `InteractionsController` branches between two services, `ChatController` builds a request) | Yes, without the exceptions |
| Response shape | **Inconsistent**: CRM controllers return the whole `ServiceActionResult<T>` envelope (for the offline `CacheableSource`); Core controllers return `.Result` | Pick one — see D2 |
| Authorization | Bare `[Authorize]` everywhere (its own plan records this as a gap) | **No** — keep permission policies |
| Auth | JWT + rotating refresh tokens (`api/auth/login|refresh|revoke`, already in DevCoreApp); `AuthTokenHandler` `DelegatingHandler` adds the bearer and refreshes once on 401; tokens kept in IndexedDB + an in-memory singleton | **Yes** |
| Client calls | Raw `HttpClient`; `IHttpApiContextFactory` is registered but **no service uses `IApiContext<T>`** | **No** — use BlazorToolkit `IApiContext<T>` |
| Dates | **Not UTC.** Server decorators convert to the profile time zone (`ToLocal(UserTimeZone)`); the client treats values as already local | **No** — this is exactly what we are replacing |
| Hosting | Client published into `wwwroot/mobile`, served by `UseBlazorFrameworkFiles("/mobile")` + `MapFallbackToFile`; CORS policy `WasmClient` from `Cors:AllowedOrigins` | **Yes** (mobile), adapted for desktop |
| OpenAPI | None | Out of scope; can be added later |

### 2.2 DevCoreApp inventory

- **~20 Admin pages** and **6 Account pages**, all Blazor Server (the Account pages are static SSR).
  Services have **no Blazor/circuit dependencies** (no `NavigationManager`, `IJSRuntime`,
  `AuthenticationStateProvider`), so they can be called from controllers as they are. The one
  exception is `AccountService`, which uses `SignInManager` (cookie) and `IHttpContextAccessor`,
  and whose callers pass absolute link bases built with `NavigationManager`.
- **Controllers exist only for** auth, files, profile pictures, part of import/export, and the
  user profile. There are **no controllers** for roles, organizations, email log, jobs, audit log,
  feature flags, API keys, webhooks, settings, theme, notifications, grid profiles, account
  flows, or current-user info.
- **Error contract mismatch.** BlazorToolkit's `HttpApiContext` reads error bodies as a
  `ServiceActionError` (`ErrorType`, `Message`, `PropertyName`). WebServiceToolkit's
  `HandleWebRequestAsync` returns `NotFound()`/`Conflict()` with no body and
  `BadRequest(string)`, and `ApiExceptionHandler` writes `ApiErrorResponse {Status, Message,
  CorrelationId}`. None of these are what the client parses, so they must be aligned (D2).
- **Service failures are return values, not exceptions.** A `ServiceActionResult.Failed(...)`
  returned to a controller must be turned into an HTTP error somewhere. It belongs in one shared
  place (the base controller), not in each action.
- **Permissions never reach a client.** The JWT carries `sub`, `email` and roles only. Permission
  claims are added per request by `PermissionClaimsTransformation`. A `GET /api/me` endpoint is
  needed.
- **Dates**: every DTO uses `DateTime`, services write `DateTime.UtcNow`, and the UI renders raw
  UTC. That makes it the easiest of the three repos to convert.
- **Existing client** (`DevCoreApp.Client`): template leftovers; `PersistentAuthenticationStateProvider`
  is always anonymous in a standalone app; the HttpClient name registered in `Program.cs` does not
  match the name passed to `HttpApiContextFactory`; no service assembly is registered.
  `Client.Services` has a generic `CRUDService<T>` over `IApiContext<T>` and a
  `NotificationHubClient` with no access-token provider.
- **Bugs to fix along the way:**
  - `UserProfileController` returns the envelope instead of `.Result`, and puts `Admin.Users.View`
    on "my profile".
  - `copyTextToClipboard` is called from `EditUser` but never defined.
  - `AccountService.LoginAsync` skips the status and lockout checks that `JwtAuthService` makes.
  - `ProfilePictureController` has no per-user authorization check.
  - Mock mode lacks `IOrganizationService`.
  - The `user/settings` link in `MainLayout` has no page.
  - The cookie is configured with `HttpOnly=false`.

## 3. Decisions (confirmed)

**D1 — Hosting.** WebService serves **Desktop at `/`** (same origin → no CORS, no token leakage
across origins) and **Mobile at `/mobile`** (the ThreadIQ `MobileClientHosting` pattern, moved to
`Core`). Both are published into `wwwroot/` by the build rather than referenced as projects, so the
server stays free of client dependencies (the ThreadIQ model). CORS stays configurable
(`Cors:AllowedOrigins`) for clients served from another origin.

**D2 — Wire contract.**
- Success → **bare `T`** (what `IApiContext<T>` deserializes). No envelope on the wire.
- Error → HTTP status + a **`ServiceActionError`** JSON body (the type the client already parses),
  with `CorrelationId` added via ProblemDetails-style extension or a derived type in `Shared.Model`.
- Shared plumbing lives in a new `Core/Controllers/ApiControllerBase`:
  - `HandleServiceAsync(() => service.X(...))` awaits a `ServiceActionResult<T>` and returns
    `.Result` on success. On failure it maps `ErrorType`/`IsAuthorized` to 400/401/403/404/409/422.
  - Every action calls this helper. That is the "common error handling"; the controller itself
    stays one line.
  - `ApiExceptionHandler` writes the same `ServiceActionError` shape for thrown exceptions.
- ThreadIQ's mobile CRM endpoints return the envelope for BlazorToolkit `CacheableSource`.
  **Decided:** change `CacheableSource` to read bare `T` (a BlazorToolkit change), so ThreadIQ drops
  the envelope too.

**D3 — Authentication.** JWT access token + rotating refresh token (existing `JwtAuthService`), for
both clients. The client uses `AuthTokenHandler` (`DelegatingHandler`, refresh-once-on-401 as in
ThreadIQ) and a `JwtAuthenticationStateProvider` fed by `GET /api/me`. Tokens live in memory, with
the refresh token persisted in `localStorage`.
- **Decided: JWT for both** clients — one auth model.
- The Blazor Server cookie UI path (`SignInManager` in `AccountService`, Identity razor
  components, `IdentityRevalidatingAuthenticationStateProvider`, logout minimal API) is removed.
- The Smart scheme stays: Bearer / `X-Api-Key` / cookie. Cookie is still needed only if the setup
  page signs in, which D5 avoids, so the cookie branch can go.

**D4 — Authorization.** Controllers carry the same `[Authorize(Policy = "...")]` the pages carry
today; the server is the only enforcement point. `GET /api/me` returns
`{ profile, roles, permissions[], organizations, timeZoneId, theme }` so the client can hide what
the user can't do (a client `PolicyAuthorizationHandler` that reads the permission list, so
`<AuthorizeView Policy="...">` keeps working unchanged in ported pages).

**D5 — Owner setup page.**
- `Pages/Setup.cshtml` (Razor Page, anonymous) at `/setup`.
- Returns 404 once `HasUsersAsync()` is true; the service re-checks inside `SetupOwnerAsync`
  as it does today.
- Posts to `AccountService.SetupOwnerAsync` **without** signing in, then redirects to `/login`
  on the Desktop client.
- Antiforgery on, and a `TimeZoneId` field (defaulted from the browser via a tiny script).
- **Decided:** the "no users exist yet" gate is sufficient; no setup token.

**D6 — Date/time.**
- DTOs keep `DateTime` (no `DateTimeOffset` churn).
- A **`UtcDateTimeJsonConverter`** in `Shared.Utils` is registered in **both** server
  `AddControllers().AddJsonOptions` and client `JsonSerializerOptions`:
  - it writes ISO-8601 with `Z`;
  - on read it forces `Kind=Utc`;
  - `Kind=Local` values are converted to UTC on write; offset-less values are read as UTC (implemented: `Shared.Utils/Core/Json/UtcDateTimeJsonConverter.cs`).
- Server rule: services and decorators never convert to a user's zone. `BaseService.UserTimeZone`
  and `ICurrentUserContext.NowLocal` stay only for non-wire uses (email text, report file names).
  Date-only values such as a due date use `DateOnly`.
- Client:
  - `ILocalTimeService` resolves the zone: the profile `TimeZoneId` if set, else the browser zone
    (`TimeZoneInfo.Local` is the browser's in WASM).
  - Display goes through `<LocalTime Value="..." Format="g" />` and `ToLocal()` helpers.
  - Date inputs convert local→UTC inside the **client service** before sending, never in pages.
  - Query parameters (e.g. audit log date ranges) are UTC too.

**D7 — Project layout.** The user text said "Clients folder"; the repo uses `src/Client/`, so it
stays:

```
src/Client/
├── Client.Services/            (existing) → DevInstance.DevCoreApp.Client.Services
│   └── Core/<Feature>/         API client services shared by Desktop + Mobile
├── DevCoreApp.Client.Desktop/  (new)  → DevInstance.DevCoreApp.Client.Desktop
│   ├── Core/UI/{Layout,Components,Pages/<Feature>}, Core/Auth, Core/Time
│   ├── App/  (empty marker)
│   └── Program.cs, UI/App.razor, _Imports.razor, wwwroot/, Styles/, Scripts/
└── DevCoreApp.Client.Mobile/   (renamed from DevCoreApp.Client) → DevInstance.DevCoreApp.Client.Mobile
```

- Shared UI (HDataGrid, PermissionGrid…) starts in Desktop. A `Client.UI` Razor class library is
  extracted **only** when Mobile actually needs a component.
- SCSS/TS build (DartSassBuilder, `tsc`) and `app.js`/`theme.js` move to Desktop.
  `reconnectModal` is deleted.

**D8 — Mocks.** Server mocks (`[BlazorServiceMock]` in `mocks/Server/...`) stay for API-level dev.
New **client mocks** `mocks/Client/Desktop.ServicesMocks` implement the client service interfaces,
so `dotnet run -c ServiceMocks` on Desktop needs no server at all. Most existing mock data
generators (Bogus) can be moved over.

## 4. API surface (target)

Each is `Core/Controllers/<Name>Controller.cs : ApiControllerBase`; one service method per action.
List endpoints take a `[QueryModel]` (WebServiceToolkit) instead of loose `top/page/sortBy/search`.

| Route | Service | Notes |
|---|---|---|
| `api/auth` (login, refresh, revoke) | `IJwtAuthService` | exists; IP/UA read in controller — comment why |
| `api/account` (register, forgot-password, reset-password, confirm-email, set-password) | `IAccountService` (new interface) | link bases built in service from `App:PublicBaseUrl` config, not from the request |
| `api/me` (GET, PUT profile, GET/PUT theme, GET permissions) | `ICurrentUserService` (new, wraps profile + permissions + theme) | replaces `UserProfileController` |
| `api/users` + sub-resources (`/{id}/organizations`, `/permission-overrides`, `/effective-permissions`, `/access-state`, `/resend-invitation`, `/password`, `/profile-picture`) | `IUserProfileService` | picture endpoints fold in `ProfilePictureController`, and gain an authorization check |
| `api/roles` (+ `/{id}/permissions`), `api/permissions` | `IRoleManagementService` | |
| `api/organizations` (+ `/tree`, `/current`, `/{id}/toggle-active`, `/{id}/move`) | `IOrganizationService` | |
| `api/email-logs` (+ `/{id}/resend`, `/resend-failed`, `DELETE ?ids=`) | `IEmailLogService` | |
| `api/jobs` (+ `/{id}/logs`, `/{id}/cancel`, `/{id}/retry`) | `IJobDashboardService` | |
| `api/audit-logs` | `IAuditLogService` | |
| `api/feature-flags` · `api/api-keys` · `api/webhooks` (+ `/{id}/deliveries`) · `api/settings` | respective admin services | |
| `api/import-export` (+ commit, rollback, session, entity-types, fields) | `IImportExportService` | `validate` currently deserializes `mappingsJson` in the controller → move to a model-bound multipart DTO |
| `api/notifications` (list, unread-count, mark-read, mark-all-read) | `INotificationService` | current-user overloads needed (today takes a `Guid`) |
| `api/grid-profiles/{grid}` | `IGridProfileService` (new interface) | |
| `api/files` | `IFileService` | exists; `download` returns `File` — the one documented non-JSON action |

Service changes required: interfaces for `AccountService` and `GridProfileService`;
`AccountService` returns `ServiceActionResult<T>` and drops `SignInManager`/cookie sign-in; sync
methods (`GetCurrentUser`, `GetAvailableRoles`, `GetImportFields`…) become async-returning SAR
where exposed; user-scoped notification methods resolve the user from `AuthorizationContext`
instead of a parameter.

## 5. Phases

Each phase ends green (`dotnet build`, `dotnet test`) and runnable.

**Phase 0 — Groundwork (server, no UI change)** — ✅ done (#1179)
1. ✅ WebServiceToolkit **10.3.0**: every `HandleWebRequestAsync` error returns a `WebServiceError`
   body (wire-compatible with `ServiceActionError`); `WebServiceException` base with
   `ForbiddenException` (403) / `UnprocessableEntityException` (422); no stack traces to clients;
   `AddWebServiceToolkitErrors()` for model-validation 400s.
2. ✅ `Core/Controllers/ApiControllerBase` — `HandleServiceAsync` / `HandleService` unwrap
   `ServiceActionResult<T>` (failed result → 403/400/422/500). All existing controllers moved onto it.
   `ApiExceptionHandler` uses `ControllerUtils.ToWebServiceError` + `CorrelationId`.
   `BusinessRuleException` derives from `UnprocessableEntityException`.
3. ✅ `UtcDateTimeJsonConverter` (Shared.Utils) registered on controllers, with tests.
4. ✅ `Core/Hosting/WasmClientHosting` — `WasmClients:{Desktop,Mobile}:BasePath` (empty = off;
   `/` rejected until Phase 4) and `Cors:AllowedOrigins` (empty = same-origin only).
5. ✅ Bugs fixed: `UserProfileController` returned the envelope and required `Admin.Users.View`
   for "my profile"; profile-picture upload/delete had no ownership check (now self or
   `Admin.Users.Edit`, enforced in the service); missing `copyTextToClipboard`; auth cookie
   `HttpOnly=false`.
   Deferred: `AccountService.LoginAsync` status/lockout gap (goes away with cookie sign-in in
   Phase 4); `IOrganizationService` mock (with the Phase 1 organizations controller);
   `user/settings` link (Phase 3 layout port).
   Noted, unchanged: `api/auth/login|refresh` report failure as 200 + `succeeded:false`
   (existing JWT contract the ThreadIQ mobile client relies on) — revisit with the Phase 2
   auth client.

**Phase 1 — API layer** (Blazor Server UI still works in parallel; same services)
1. Add `api/me`, `api/account`, then one controller per feature in §4, with the page's policy on each.
2. Add `[QueryModel]` list DTOs in `Shared.Model/Core/<Feature>`.
3. Add integration tests with `WebApplicationFactory` for the base controller contract (success,
   each error mapping, 401/403) and a smoke test per controller.

**Phase 2 — Client foundation**
1. Rename `DevCoreApp.Client` → `DevCoreApp.Client.Mobile` (folder, csproj, namespaces, slnx), and fix
   the HttpClient-name mismatch. Give Mobile login + profile screens that use the same
   `Client.Services` auth/`me` services as Desktop — the proof that the shared layer works.
2. Create `DevCoreApp.Client.Desktop`:
   - `Program.cs`: named HttpClient + `AuthTokenHandler` + `HttpApiContextFactory(..., "api")`, and
     `AddBlazorServices(ClientServices assembly)`;
   - `JwtAuthenticationStateProvider`, permission policy handler, `AuthorizeRouteView`;
   - `BasePage : IServiceExecutionHost`, `MainLayout`, `NavMenu`;
   - `ILocalTimeService` + `<LocalTime>`;
   - login/logout pages;
   - SignalR `NotificationHubClient` with `AccessTokenProvider`.
3. Move styles, scripts and the theme FOUC script into Desktop's `index.html`.
4. Add client mocks project and `ServiceMocks` config.

**Phase 3 — Port pages** (one PR-sized slice per feature; each slice = client service + page(s) + mocks)
- Order: Account (register/forgot/reset/confirm) → Profile/Theme → Users (list/new/edit) →
  Roles → Organizations → Settings → Feature flags → API keys → Webhooks → Email log → Jobs →
  Audit log → Import/Export → Notifications bell (now live via SignalR).
- Pages keep their markup; `@inject IXxxService` switches from server services to client services
  of the same shape. Date rendering goes through `<LocalTime>`.

**Phase 4 — Cut over**
1. Add the `Pages/Setup.cshtml` Razor Page; remove `Core/UI/**` Blazor Server pages, `UI/App.razor`,
   `UI/Routes.razor`, `AddInteractiveServerComponents`, `MapRazorComponents`,
   `IdentityRevalidatingAuthenticationStateProvider`, the Identity razor endpoints, and
   `reconnectModal`.
2. Enable client hosting and point email links (confirm, reset, invite) at Desktop routes.
3. The Blazor-Server "per-operation unit of work" rationale stays valid (controllers are
   per-request, but background/concurrent API calls benefit too). Keep `RepositoryFactory`
   and update the doc wording.

**Phase 5 — Docs & fan-out**
- Update the root `CLAUDE.md`, `src/Server/Admin/WebService/CLAUDE.md`, and add `docs/Api.md` (wire
  contract, error shape, UTC rule, base controller) and a `CLAUDE.md` for Desktop.
- Write `docs/migration/out/2026-MM-DD-wasm-client-and-api-layer.md` (see §6).

## 6. Fork migration doc (outline)

Target: ThreadIQ, Tentrie, future forks. Content:
1. **Shared surface added:**
   - `ApiControllerBase`, `ServiceActionError` contract, `UtcDateTimeJsonConverter`;
   - `Core` controllers, `ClientHosting`, `IAccountService`/`IGridProfileService`/`ICurrentUserService`;
   - the `Client.Desktop` `Core/**` tree and `Client.Services/Core/**`.
2. **Shared surface removed:** Blazor Server `Core/UI/**`, Identity components, cookie UI sign-in.
3. **Per-fork work (not automatable):**
   - **ThreadIQ**:
     - Server decorators must stop converting to `UserTimeZone`, which is a **breaking change for
       the shipped mobile client**. The mobile display code (`CrmDisplay.Relative`, etc.) must
       convert UTC→local in the same release.
     - Fold `CrmCrudControllerBase` into `ApiControllerBase`.
     - Resolve the envelope-vs-bare decision (D2).
     - Replace bare `[Authorize]` with policies.
     - Its `ThreadIQ.Client` becomes the `Client.Mobile` equivalent.
     - `App` admin pages (CRM) must be ported to Desktop.
   - **Tentrie**: has its own `Tentrie.Client` WASM + TS/SCSS pipeline and a new bUnit test project.
     It needs an assessment of which is Desktop vs Mobile before applying; `App` Blazor Server pages
     must be ported.
4. **Order:**
   - server groundwork and API first (the UI keeps working);
   - then the client;
   - then the cut-over;
   - with a checklist and verification steps per stage.

## 7. Resolved questions

| # | Question | Decision |
|---|---|---|
| Q1 | Wire format | Bare `T` on success, `ServiceActionError` on error; BlazorToolkit `CacheableSource` changes to read bare `T` |
| Q2 | Desktop auth | JWT, same as Mobile |
| Q3 | Setup protection | "No users exist yet" gate only |
| Q4 | Mobile scope | Phase 2 gives Mobile login + profile screens over the shared `Client.Services` |
| Q5 | Display time zone | Profile `TimeZoneId`, browser zone as fallback |
