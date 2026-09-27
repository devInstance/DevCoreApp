# CLAUDE.md — Project Conventions

## Architecture: API host → Services → Repository

This project is the **server**: the `/api` controllers, the SignalR hub, health checks, background
jobs, and the host for the two WASM clients (Desktop at `/`, Mobile at `/mobile`, via project
references — see `Core/Hosting/WasmClientHosting.cs`). It renders exactly two pages itself, as Razor
Pages under `Core/Pages`: the first-run owner `/setup` and `/Error`. All UI lives in
`src/Client/*` — see [`../../Client/DevCoreApp.Client.Desktop/CLAUDE.md`](../../Client/DevCoreApp.Client.Desktop/CLAUDE.md).

```
WASM client → /api controller (ApiControllerBase) → Service → Repository
```

### Service Pattern
- Define an interface (`I{Entity}Service`) for each service
- Inherit from `BaseService`
- Annotate with `[BlazorService]`
- Return `ServiceActionResult<T>` (use `ServiceActionResult<T>.OK(data)`)
- Open **one unit of work per public method** — `await using var repo = RepositoryFactory.Create();`
  — then access data via `repo.GetXxxQuery(AuthorizationContext.CurrentProfile)`. There is no
  shared scoped `Repository` property on `BaseService` any more (it was removed when concurrent
  Blazor Server components collided on a shared context; concurrent API requests need the same
  isolation). Private helpers that touch data
  take an `IQueryRepository repo` parameter instead of creating their own.
  See [`../Database/UnitOfWork.md`](../Database/UnitOfWork.md).
- Create new records via `query.CreateNew()` + `entity.ToRecord(dto)` + `query.AddAsync(record)`
- Update existing records via `entity.ToRecord(dto)` + `query.UpdateAsync(record)`
- Background work (e.g., email) via `IBackgroundWorker.Submit()`
- DI registration is automatic — `[BlazorService]` + `AddBlazorServices()` registers the class under each of its interfaces, or under its concrete type only when it implements none. Inject services by interface.

### Logging
- Create a local logger in the constructor: `log = logManager.CreateLogger(this);`
- Start each method with a trace scope: `using var l = log.TraceScope();`
- Use shorthand methods on the scope: `l.I("info message")`, `l.E("error message")`
- Do **not** use `ILogger` / `LogInformation` / `LogError` — use `IScopeLog` from `DevInstance.LogScope`

### ID Generation
- Use `IdGenerator.New()` from `DevInstance.WebServiceToolkit.Common.Tools` for generating unique public IDs and temporary values (e.g., temp passwords)

### DTOs
- DTOs (`{Entity}Item`, request types) carry validation attributes (`[Required]`, `[EmailAddress]`,
  …) directly. `[ApiController]` enforces them and answers a `WebServiceError` 400 naming the field;
  the WASM forms use the same attributes client-side.

### Naming Conventions
- **DTO / View Model:** `{Entity}Item` (e.g., `UserProfileItem`)
- **Service Interface:** `I{Entity}Service` (e.g., `IUserProfileService`)
- **Service Implementation:** `{Entity}Service` (e.g., `UserProfileService`)
- **Service Mock:** `{Entity}ServiceMock` (e.g., `UserProfileServiceMock`)
- **Decorators:** `{Entity}Decorators` — extension methods `ToView()` / `ToRecord()` for model ↔ DTO conversion
  - `ToView()` converts database model → DTO
  - `ToRecord()` maps DTO fields onto an existing database entity
- **Database Model:** `{Entity}` inheriting `DatabaseObject`

### Background Email
Queue emails via `IBackgroundWorker.Submit()` with a `BackgroundRequestItem` of type `SendEmail` containing an `EmailRequest`.

### Email Templates
- HTML templates live in `wwwroot/email-templates/`
- Template names are string constants in `EmailTemplateName`
- Template metadata (subject, path, isHtml) is registered in `EmailTemplateRepository`
- Render templates via `IEmailTemplateService.RenderAsync(name, placeholders)` — returns `EmailTemplateResult` with `Subject`, `Content`, `IsHtml`
- Placeholders use `{{Key}}` syntax in both subject and body
- To add a new template: add a constant to `EmailTemplateName`, register in `EmailTemplateRepository`, create the HTML file in `wwwroot/email-templates/`

### API Controller Pattern
- Routes are literal `[Route("api/...")]`; controllers derive from `Core/Controllers/ApiControllerBase`
  (which carries `[ApiController]`) and put the page's permission policy on the class or action.
- Each action makes **one** service call: `return HandleServiceAsync(() => _service.DoAsync(...));`
  Success returns the bare result (no `ServiceActionResult` envelope); failures and thrown
  WebServiceToolkit exceptions become a `WebServiceError` body the Blazor clients read as
  `ServiceActionError`.
- No mapping, validation or branching in controllers. When something must happen there (reading
  an `IFormFile` stream, client IP), say why in a comment.
- Every `DateTime` on the wire is UTC (`UtcDateTimeJsonConverter`); clients convert for display.
- Plan and rationale: [`docs/WasmMigrationPlan.md`](../../../docs/WasmMigrationPlan.md).

### Import/Export Engine
Generic CSV/Excel import and export for any entity type via handler pattern. Full documentation: [`../Services/Core/ImportExport/ImportExport.md`](../Services/Core/ImportExport/ImportExport.md).

### Roles
Defined in `ApplicationRoles`: Owner, Admin, Manager, Employee, Client. Owner is the super-admin role and is typically excluded from user-assignable roles.

## Service Mocks

Server mocks let the **API** run without a real database or external dependencies: the controllers
return fake data generated with [Bogus](https://github.com/bchavez/Bogus). For UI work with no
server at all, use the **client** mocks instead (`mocks/Client/Client.Services.Mocks`, Desktop
`-c ServiceMocks`) — same data generators, retargeted to the client service interfaces. A new
service normally needs both.

### Build Configuration
The solution has a `ServiceMocks` build configuration. Use it to run with mock services:
- **Visual Studio**: Select `ServiceMocks` from the configuration dropdown
- **CLI**: `dotnet build -c ServiceMocks` / `dotnet run -c ServiceMocks`

The `SERVICEMOCKS` preprocessor symbol controls which services are registered in `Program.cs`:
```csharp
#if !SERVICEMOCKS
    builder.Services.AddBlazorServices();                                   // registers [BlazorService] classes
    builder.Services.AddBlazorServices(typeof(UserProfileService).Assembly);
#else
    builder.Services.AddBlazorServicesMocks();                                      // registers [BlazorServiceMock] classes
    builder.Services.AddBlazorServicesMocks(typeof(UserProfileServiceMock).Assembly); // from mocks assembly
    builder.Services.AddBlazorServicesMocks(typeof(UserProfileService).Assembly);    // dual-annotated services from real assembly
#endif
```

### Project Structure
```
mocks/Server/Services.Mocks/
├── DevCoreApp.Server.Services.Mocks.csproj   # References real services project + Bogus
├── UserAdmin/
│   └── UserProfileServiceMock.cs            # Mock for IUserProfileService
└── Email/
    └── EmailLogServiceMock.cs               # Mock for IEmailLogService
```

### Creating a New Mock
1. Create `{Entity}ServiceMock.cs` in the appropriate subfolder under `mocks/Server/Services.Mocks/`
2. Implement the service interface (e.g., `IUserProfileService`)
3. Annotate with `[BlazorServiceMock]` (not `[BlazorService]`)
4. Generate fake data in the constructor using Bogus `Faker<T>`
5. Store data in an in-memory `List<T>` and operate on it
6. Add `await Task.Delay(delay)` in async methods to simulate latency

```csharp
[BlazorServiceMock]
public class UserProfileServiceMock : IUserProfileService
{
    const int TotalCount = 100;
    List<UserProfileItem> modelList;
    private int delay = 500;

    public UserProfileServiceMock()
    {
        var faker = new Faker<UserProfileItem>()
            .RuleFor(u => u.Id, f => IdGenerator.New())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName());
        modelList = faker.Generate(TotalCount);
    }

    // Implement interface methods operating on modelList...
}
```

### Dual-Annotated Services
Services that have no mock and should work in both modes (e.g., `GridProfileService`, `AccountService`) must carry **both** attributes:
```csharp
[BlazorService]
[BlazorServiceMock]
public class GridProfileService : BaseService { ... }
```
This ensures they are registered by both `AddBlazorServices()` and `AddBlazorServicesMocks()`.
