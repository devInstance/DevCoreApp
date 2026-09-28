# API wire contract

Every UI talks to the server only through `/api` (plus the SignalR hub). This page is the
contract that the server (`src/Server/Api`) and the clients (`src/Client/Client.Services`) both
implement. `tests/Server/Api/Core/Api/ApiContractTests.cs` pins it through a real pipeline, so a
change here must change those tests too.

Design history: [`WasmMigrationPlan.md`](WasmMigrationPlan.md) (decisions D1–D8).

## Responses

| Outcome | Status | Body |
|---|---|---|
| Success | `200` | the **bare** result: an item, a `PagedList<T>`, `true`, or `null`. There is no envelope |
| Validation failure (model binding or service) | `400` | `WebServiceError`, with `PropertyName` when known |
| Not authenticated / token expired | `401` | empty. The client refreshes once and replays |
| Authenticated but not permitted | `403` | `WebServiceError` |
| Not found | `404` | `WebServiceError` |
| Conflict | `409` | `WebServiceError` |
| Business rule (`BusinessRuleException`, general service error) | `422` | `WebServiceError` |
| Unexpected | `500` | `WebServiceError` (message hidden outside Development) |

`null` is sent as JSON `null` with a `200`, not as a `204`: `AddApiControllers()` removes the
no-content formatter.

The error body is WebServiceToolkit's `WebServiceError`, extended with the correlation id:

```json
{ "errorType": 3, "message": "Email is required.", "propertyName": "Email", "correlationId": "0HN…" }
```

`errorType` is `Unknown = 0`, `Exception = 1`, `General = 2`, `Validation = 3`: the numbering of
BlazorToolkit's `ServiceActionErrorType` since **10.4**. Clients deserialize it straight into
`ServiceActionError`, so a client on an older BlazorToolkit misreads every error type.
`correlationId` matches the `CorrelationId` column in the server log.

### Server side

Controllers derive from `ApiControllerBase` and make **one** service call per action:

```csharp
[HttpGet]
[Authorize(Policy = PermissionDefinitions.Admin.ApiKeys.View)]
public Task<ActionResult<PagedList<ApiKeyItem>>> GetListAsync([FromQuery] ListQuery query)
    => HandleServiceAsync(() => _service.GetKeysAsync(query.Top, query.Page, query.SortBy, query.Search));
```

`HandleServiceAsync` unwraps a successful `ServiceActionResult<T>` into the bare result. It turns a
failed one into the matching WebServiceToolkit exception (not authorized → 403, validation → 400,
general → 422, other → 500). Exceptions thrown by services (`BadRequestException`,
`RecordNotFoundException`, `ForbiddenException`, `BusinessRuleException`, …) take the same path.
Anything that escapes MVC is shaped by `ApiExceptionHandler`.

### Client side

Client services derive from `ApiServiceBase`. `CallAsync(...)` turns the response, or the
`WebServiceError`, back into a `ServiceActionResult<T>`, so pages use `IServiceExecutionHost`
exactly as before.

## Lists and queries

List endpoints return `PagedList<T>` (`Shared.Model.Core.Common`), which is BlazorToolkit's
`IModelList<T>`:

```json
{ "totalCount": 42, "pagesCount": 3, "page": 0, "count": 20, "sortOrder": ["-CreateDate"], "search": null, "items": [ … ] }
```

Query parameters bind to `[QueryModel]` classes deriving from `ListQuery`:

| Parameter | Meaning |
|---|---|
| `top` | page size, default 20 |
| `page` | zero-based page |
| `sortBy` | comma-separated fields in priority order: `Name` ascending, `-Name` descending. **No `+` prefix**, because it decodes to a space |
| `search` | free-text filter |
| `startDate`, `endDate` | `DateRangeListQuery` only; UTC |

Feature queries (`AuditLogQuery`, `JobQuery`, `EmailLogQuery`, `OrganizationQuery`,
`SettingQuery`) add their own filters. Clients build the query string with `.Query(model)` from
`ApiQueryExtensions`, which escapes values and formats them invariantly. Never hand-build
`Parameter(...)` strings.

IDs in routes are always `PublicId`s. The Guid primary key never leaves the server.

## Dates

**Every `DateTime` on the wire is UTC.**

- The server serializes with `UtcDateTimeJsonConverter`: always ISO 8601 with a `Z`. Incoming
  values without an offset are read as UTC.
- Clients never display a raw API date. They go through `ILocalTimeService`, the `<LocalTime>`
  component or `LocalClock` in Desktop.
- The display zone is the profile's `TimeZoneId`, falling back to the browser's zone.
- Dates the user enters are converted with `ILocalTimeService.ToUtc` **inside the client service**
  before they are sent.

## Authentication

| Traffic | Credential |
|---|---|
| `/api/**` from the WASM clients | `Authorization: Bearer <JWT>` |
| `/api/**` from integrations | `X-Api-Key: <key>` |
| `/hubs/notifications` | `?access_token=<JWT>` (WebSockets cannot send headers) |

The default scheme is the `Smart` policy scheme, which picks one per request: bearer or hub token →
JWT, `X-Api-Key` → ApiKey, anything else → JWT (so a missing token is a clean 401). No UI signs in
with the Identity cookie any more.

| Endpoint | Purpose |
|---|---|
| `POST api/auth/login` | email + password → access token, refresh token, expiries |
| `POST api/auth/refresh` | rotates the refresh token. Reusing an already-rotated token revokes the whole family, so clients refresh **single-flight** (`TokenRefresher`) |
| `POST api/auth/revoke` | logout |
| `GET api/me` | `CurrentUserItem`: profile, roles, effective permissions, theme, `realTimeNotifications`. Clients build their `AuthenticationState` from it |
| `GET/PUT api/me/profile`, `GET/PUT api/me/theme` | the signed-in user's own data |
| `api/account/*` | anonymous: `register`, `forgot-password`, `reset-password`, `confirm-email`, `set-password` (invitation; re-verifies the emailed token), `setup-required` |

Authorization is per permission: `[Authorize(Policy = "Module.Entity.Action")]` on each action,
built on demand by `PermissionPolicyProvider`. By convention GET → `View`, POST → `Create`, PUT →
`Edit`, DELETE → `Delete`. Clients read the same keys from `api/me` to show or hide UI, but
only the server enforces them.

## Binary endpoints

File downloads, exports and profile pictures return a file stream, not JSON. Clients call them with
the named `HttpClient` (`ApiClient.HttpClientName`), which still carries the bearer token. `<img
src="api/...">` cannot send a token, so pictures are fetched by the client service and bound as
`data:` URLs.

## Real-time notifications

`Notifications:RealTime` (default `true`) decides whether `/hubs/notifications` is mapped at all.
`api/me` reports the setting, and clients use `IUnreadNotificationsMonitor`: the REST count first,
then the hub when offered, and polling every 60 s whenever the hub is off or disconnected. Turn it
off when running several instances without a SignalR backplane.

## Hosting and CORS

The Api host serves both clients from the same origin: Desktop at `/`, Mobile at `/mobile/`
(`WasmClientHosting`). Same-origin clients need no CORS. A client deployed on another origin is
listed in `Cors:AllowedOrigins`. Unknown `/api`, `/hubs` and `/health` paths return 404, never the
SPA's `index.html`.

The server renders only two pages itself, as Razor Pages: `/setup`, the first-run owner
creation, which returns 404 once any user exists, and `/Error`.
