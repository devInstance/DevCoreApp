# Contributing: adding a feature end to end

A feature crosses seven layers, from the database to the WASM page. This guide follows one real
feature, **API keys**, through all of them. Copy its files when you add yours.

Put product features under `App/` and shared template features under `Core/`. The paths below use
`Core/`; see the root [`CLAUDE.md`](../../../CLAUDE.md) for the rule.

```
Desktop page ─► client service ─HTTP─► controller ─► server service ─► query ─► DbContext
 (Client.Desktop)  (Client.Services)    (Server.Api)   (Server.Services)  (Database.Core)
        ▲                                                     │
        └──────────── Shared.Model DTO (ApiKeyItem) ◄─────────┘ decorator ToView()
```

Also read: [`docs/Api.md`](../../../docs/Api.md) (wire contract),
[`CLAUDE.md`](CLAUDE.md) (server conventions), the Desktop
[`CLAUDE.md`](../../Client/DevCoreApp.Client.Desktop/CLAUDE.md) (pages, dates) and
[`UnitOfWork.md`](../Database/UnitOfWork.md).

## 1. Entity: `Database/Core/Models/ApiKey.cs`

- Inherit `DatabaseEntityObject` for user-tracked business data, `DatabaseObject` for API-exposed
  data without user tracking, or `DatabaseBaseObject` for infrastructure.
- Implement `IOrganizationScoped` on business data. Do **not** set `OrganizationId` yourself; the
  interceptor stamps it.
- Mark secrets `[AuditExclude]`.
- Product entities go in `Database/Core/App/Models/<Entity>` (there is no `Core.Core`).
- **A schema change needs a migration in both `Database/Postgres` and `Database/SqlServer`.** Do
  not scaffold it: tell the maintainer a migration is needed.

## 2. Query: `Database/Core/Data/Queries/`

- Interface `IApiKeyQuery : IModelQuery<ApiKey, IApiKeyQuery>` (+ `IQSearchable`, `IQSortable`,
  `IQPageable` as needed), with the implementation `BasicsImplementation/CoreApiKeyQuery.cs`.
- Add `GetApiKeyQuery(UserProfile currentProfile)` to `IQueryRepository` and implement it in
  `CoreQueryRepository`.
- `CreateNew()` on the query is the only way to create an entity outside seeders.

## 3. DTO and decorator

- `Shared/Model/Core/ApiKeys/ApiKeyItem.cs`: implements `IModelItem`. `Id` is the entity's
  **`PublicId`**. Validation attributes (`[Required]`, …) go here, and both the API and the WASM
  forms enforce them.
- `Database/Core/Data/Decorators/ApiKeyDecorators.cs`: `ToView()` (entity → DTO) and
  `ToRecord(dto)` (DTO → entity). Pure mapping; dates stay UTC.

## 4. Server service: `Server/Services/Core/ApiKeys/`

```csharp
[BlazorService]
public class ApiKeyAdminService : BaseService, IApiKeyAdminService
{
    public async Task<ServiceActionResult<PagedList<ApiKeyItem>>> GetKeysAsync(
        int top, int page, string[]? sortBy = null, string? search = null)
    {
        using var l = log.TraceScope();

        await using var repo = RepositoryFactory.Create();          // one unit of work per method
        var query = repo.GetApiKeyQuery(AuthorizationContext.CurrentProfile);
        // search, sort, count, page …
        var items = keys.Select(ak => ak.ToView()).ToArray();
        return ServiceActionResult<PagedList<ApiKeyItem>>.OK(
            PagedList.Create(items, totalCount, top, page, sortBy, search));
    }
}
```

- Interface first (`IApiKeyAdminService`). `[BlazorService]` registers the class **only under its
  interfaces**, so always inject the interface.
- Return `ServiceActionResult<T>`. For expected failures throw WebServiceToolkit exceptions:
  - `BadRequestException(message, propertyName)` → 400;
  - `RecordNotFoundException` → 404;
  - `ForbiddenException` → 403;
  - `BusinessRuleException` → 422.

  Never throw `Exception` / `InvalidOperationException` for these.
- Check **ownership** in the service when a user may only touch their own records. The controller's
  permission check is not enough.
- Log with `IScopeLog` (`log.TraceScope()`, `l.I(...)`), never `ILogger`.
- Add a `[BlazorServiceMock]` twin in `mocks/Server/Services.Mocks/Core/<Feature>`, which is
  compiled into the Api host by `-c ServiceMocks`.

## 5. Permission: `Shared/Model/Core/Permissions/PermissionDefinitions.cs`

```csharp
public static class ApiKeys
{
    public const string View = "Admin.ApiKeys.View";
    public const string Create = "Admin.ApiKeys.Create";
    public const string Revoke = "Admin.ApiKeys.Revoke";
}
```

`PermissionSeeder` picks the constants up on startup, and `PermissionPolicyProvider` builds the
policies. There is no `AddPolicy` call to write. Assign the permission to roles in the seeder or
the Roles page.

## 6. Controller: `Server/Api/Core/Controllers/ApiKeysController.cs`

```csharp
[Route("api/api-keys")]
[Authorize]
public class ApiKeysController : ApiControllerBase
{
    private readonly IApiKeyAdminService _service;

    public ApiKeysController(IApiKeyAdminService service) => _service = service;

    [HttpGet]
    [Authorize(Policy = PermissionDefinitions.Admin.ApiKeys.View)]
    public Task<ActionResult<PagedList<ApiKeyItem>>> GetListAsync([FromQuery] ListQuery query)
        => HandleServiceAsync(() => _service.GetKeysAsync(query.Top, query.Page, query.SortBy, query.Search));

    /// <summary>The plain-text key is only in this response; it is never retrievable again.</summary>
    [HttpPost]
    [Authorize(Policy = PermissionDefinitions.Admin.ApiKeys.Create)]
    public Task<ActionResult<ApiKeyCreateResult>> CreateAsync([FromBody] ApiKeyItem item)
        => HandleServiceAsync(() => _service.CreateKeyAsync(item));

    [HttpPost("{id}/revoke")]
    [Authorize(Policy = PermissionDefinitions.Admin.ApiKeys.Revoke)]
    public Task<ActionResult<bool>> RevokeAsync(string id)
        => HandleServiceAsync(() => _service.RevokeKeyAsync(id));
}
```

- Use a literal kebab-case `api/...` route, with a permission per action (GET → View, POST → Create,
  PUT → Edit, DELETE → Delete).
- Make **one service call per action** and do nothing else: no mapping, validation, branching or
  `try/catch`. If a controller must do something (read an `IFormFile` stream, pass the client IP),
  explain it in a comment.
- List endpoints take `[FromQuery] ListQuery`, or a `[QueryModel]` class deriving from it or from
  `DateRangeListQuery` for filters. Put new query models in `Shared/Model/Core/<Feature>`.
- Route ids are `PublicId`s.

## 7. Client service: `Client/Client.Services/Core/ApiKeys/`

The interface mirrors the server's, with the same name and signatures, so the page code is the
same either way:

```csharp
[BlazorService]
public class ApiKeyAdminService : ApiServiceBase, IApiKeyAdminService
{
    public ApiKeyAdminService(IHttpApiContextFactory apiFactory, IScopeManager logManager) : base(apiFactory, logManager) { }

    public Task<ServiceActionResult<PagedList<ApiKeyItem>>> GetKeysAsync(int top, int page, string[]? sortBy = null, string? search = null) =>
        CallAsync(() => Api<ApiKeyItem>("api/api-keys").Get()
            .Query(new ListQuery { Top = top, Page = page, SortBy = sortBy!, Search = search! })
            .ExecuteAsync<PagedList<ApiKeyItem>>());

    public Task<ServiceActionResult<bool>> RevokeKeyAsync(string id) =>
        CallAsync(() => Api<bool>($"api/api-keys/{Segment(id)}/revoke").Post<object?>(null).ExecuteAsync());
}
```

- `CallAsync` turns the response, or the server's `WebServiceError`, into a `ServiceActionResult<T>`.
- Query strings go through `.Query(model)`, and path values through `Segment(id)`. Both escape the
  value, and `.Query` also formats it invariantly.
- Dates the user typed are local: convert them with `ILocalTimeService.ToUtc` **here**, before
  sending.
- Business logic that belongs to the client (combining calls, caching) lives in the client service,
  not in the page.
- Add a `[BlazorServiceMock]` twin in `mocks/Client/Client.Services.Mocks/Core/<Feature>`. It is
  what `dotnet run -c ServiceMocks` on Desktop uses.
- Add tests in `tests/Client/Client.Services.Tests` for anything beyond a straight call.

## 8. Page: `Client/DevCoreApp.Client.Desktop/Core/UI/Pages/Admin/ApiKeysPage.razor(.cs)`

```razor
@page "/admin/api-keys"
@attribute [Authorize(Policy = "Admin.ApiKeys.View")]

<AuthorizeView Policy="Admin.ApiKeys.Create">
    <button class="btn btn-primary" @onclick="ShowCreateModal" disabled="@Host.InProgress">New API Key</button>
</AuthorizeView>
```

```csharp
public partial class ApiKeysPage
{
    [Inject] private IApiKeyAdminService KeyService { get; set; } = default!;
    [CascadingParameter] private IServiceExecutionHost Host { get; set; } = default!;

    private async Task LoadKeys(int page, string[]? sortBy, string? search) =>
        await Host.ServiceReadAsync(
            async () => await KeyService.GetKeysAsync(pageCount, page, sortBy, search),
            result => KeyList = result);
}
```

- Pages inject **client** service interfaces and call them through `IServiceExecutionHost`
  (`ServiceReadAsync` / `ServiceSubmitAsync`), which drives `Host.InProgress`, `Host.IsError` and
  `Host.ErrorMessage`.
- A page never touches `HttpClient`, the API URL or tokens.
- Permissions in `[Authorize]` / `<AuthorizeView>` only show or hide UI; the API enforces them.
  Nested `<AuthorizeView>`s need distinct `Context` names.
- **Never display a raw `DateTime`.** Use `<LocalTime Value="…" />` or `LocalClock.Format(…)`.
- Grids use `HDataGrid` with `ColumnDescriptor<T>` and a grid profile. See
  [`HDataGrid.md`](../../Client/DevCoreApp.Client.Desktop/Core/UI/Components/HDataGrid.md).
- Add the link to `Core/UI/Layout/NavMenu.razor` inside an `<AuthorizeView Policy="…">`.

## Checklist

- [ ] Entity (+ `IOrganizationScoped`) → tell the maintainer a migration is needed for **both**
      providers
- [ ] Query + `IQueryRepository.Get{Entity}Query` + `CoreQueryRepository`
- [ ] DTO (`IModelItem`, validation attributes) + decorators
- [ ] Server service + interface + server mock
- [ ] Permission constants
- [ ] Controller on `ApiControllerBase`, one call per action, policy per action
- [ ] Client service + interface + client mock (+ tests)
- [ ] Desktop page + nav link (and Mobile screen if it applies)
- [ ] `dotnet build DevInstance.DevCoreApp.slnx` **and** `-c ServiceMocks`, then `dotnet test`
- [ ] Run the Api host and exercise the page: the server log shows only 2xx for its `/api` calls

## Running

```bash
dotnet run --project src/Server/Api/DevCoreApp.Server.Api.csproj                              # server + both clients
dotnet run -c ServiceMocks --project src/Client/DevCoreApp.Client.Desktop/DevCoreApp.Client.Desktop.csproj   # UI on mocks, no server
```
