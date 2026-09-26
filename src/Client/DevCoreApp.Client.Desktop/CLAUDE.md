# CLAUDE.md — Desktop client (Blazor WebAssembly admin UI)

The admin UI. Runs entirely in the browser and talks to the server only through `/api` (JWT).
Plan and rationale: [`docs/WasmMigrationPlan.md`](../../../docs/WasmMigrationPlan.md). Root
conventions (Core/App split, naming) are in the root [`CLAUDE.md`](../../../CLAUDE.md).

## Run

```bash
# Against the API (start the WebService first; ApiBaseUrl is in wwwroot/appsettings.Development.json)
dotnet run --project src/Client/DevCoreApp.Client.Desktop/DevCoreApp.Client.Desktop.csproj   # http://localhost:5280

# No server at all: in-memory mocks (mocks/Client/Client.Services.Mocks); sign in with anything
dotnet run -c ServiceMocks --project src/Client/DevCoreApp.Client.Desktop/DevCoreApp.Client.Desktop.csproj
```

## Pages → client services → /api

- Pages never touch HTTP. They inject a **client service interface** from
  `Client.Services/Core/<Feature>` and call it through `IServiceExecutionHost`
  (`Host.ServiceReadAsync` / `Host.ServiceSubmitAsync`), exactly as the server pages did.
- Client service interfaces mirror the server service interfaces (same names and signatures), so
  a page ports by changing `using`s. Where the server method was synchronous, the client one is
  `…Async` (it is an HTTP call).
- A client service derives from `ApiServiceBase`: `CallAsync(() => Api<T>("api/...").Get()...)`
  turns the response or the server's `ServiceActionError` into a `ServiceActionResult<T>`. Query
  models go through `.Query(model)` (encoded for you), never raw `Parameter(...)` strings.
- Binary endpoints (files, pictures) use the named `HttpClient` (`ApiClient.HttpClientName`)
  directly — the bearer token is still attached by `AuthTokenHandler`.
- Every client service needs a `[BlazorServiceMock]` twin in `mocks/Client/Client.Services.Mocks`.

## Dates and times

Everything from the API is **UTC**. Never `ToString()` an API `DateTime` in a page:

- markup: `<LocalTime Value="item.CreateDate" Format="g" />` or `@LocalClock.Format(item.CreateDate, "g")`
- grid columns / code: `LocalClock.Format(value, "g", "-")`
- date inputs: pass the picked (local) value to the client service; the service converts it with
  `ILocalTimeService.ToUtc` before calling the API.

The zone is the user's profile `TimeZoneId`, falling back to the browser's.

## Authorization

- `[Authorize(Policy = PermissionDefinitions.X.Y.Z)]` on pages and `<AuthorizeView Policy="…">` in
  markup use the permissions from `api/me` — for showing/hiding only; the API enforces the same
  keys. Use the permission the page's API calls need (GET → `View`, POST → `Create`, …).
- Nested `<AuthorizeView>`s need distinct `Context` names.

## Pictures and other authenticated resources

`<img src="api/...">` sends no bearer token. Load through the service
(`IProfilePictureService.GetDataUrlFromPathAsync`) and bind the returned `data:` URL.
