# CLAUDE.md — Project Conventions

## Architecture: Pages → Services → Repository

Pages must **never** access the database directly. All operations go through services via `IServiceExecutionHost`.

```
Page (Blazor) → Host.ServiceReadAsync / Host.ServiceSubmitAsync → Service → Repository
```

### Page Pattern
- Inject the service **via interface**: `[Inject] private IUserProfileService UserService { get; set; } = default!;`
- Receive the host as cascading parameter: `[CascadingParameter] private IServiceExecutionHost Host { get; set; } = default!;`
- Use `Host.ServiceReadAsync(async () => await Service.Method(), result => Property = result)` for reads
- Use `Host.ServiceSubmitAsync(async () => await Service.Method())` for writes
- Do **not** manage `IsSubmitting` / loading state manually — BlazorToolkit manages `Host.InProgress`
- Use `Host.InProgress`, `Host.IsError`, `Host.ErrorMessage` for UI state
- Do **not** inject `ApplicationDbContext`, `IUserStore`, `ITimeProvider`, or `IBackgroundWorker` into pages

### Service Pattern
- Define an interface (`I{Entity}Service`) for each service
- Inherit from `BaseService`
- Annotate with `[BlazorService]`
- Return `ServiceActionResult<T>` (use `ServiceActionResult<T>.OK(data)`)
- Access data via `Repository.GetXxxQuery(AuthorizationContext.CurrentProfile)`
- Create new records via `query.CreateNew()` + `entity.ToRecord(dto)` + `query.AddAsync(record)`
- Update existing records via `entity.ToRecord(dto)` + `query.UpdateAsync(record)`
- Background work (e.g., email) via `IBackgroundWorker.Submit()`
- DI registration is automatic — `[BlazorService]` + `AddBlazorServices()` registers both the concrete type and its interfaces

### Logging
- Create a local logger in the constructor: `log = logManager.CreateLogger(this);`
- Start each method with a trace scope: `using var l = log.TraceScope();`
- Use shorthand methods on the scope: `l.I("info message")`, `l.E("error message")`
- Do **not** use `ILogger` / `LogInformation` / `LogError` — use `IScopeLog` from `DevInstance.LogScope`

### ID Generation
- Use `IdGenerator.New()` from `DevInstance.WebServiceToolkit.Common.Tools` for generating unique public IDs and temporary values (e.g., temp passwords)

### DTO / Form Model Pattern
- DTOs (`{Entity}Item`) carry validation attributes (`[Required]`, `[EmailAddress]`, `[Phone]`, `[Display]`) directly — no separate `InputModel` classes in pages
- Pages bind forms directly to the DTO: `[SupplyParameterFromForm] private UserProfileItem Input { get; set; } = new();`
- Fields not part of the DTO (e.g., role selection during creation) live as separate page properties

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

### HDataGrid Component
Use `HDataGrid<TItem>` for all tabular data pages. Do not write inline `<table>` markup. Full documentation: [`UI/Components/HDataGrid.md`](UI/Components/HDataGrid.md).

### Roles
Defined in `ApplicationRoles`: Owner, Admin, Manager, Employee, Client. Owner is the super-admin role and is typically excluded from user-assignable roles.

## Service Mocks

Service mocks allow running the application without a real database or external dependencies. They are used for UI development and testing.

### Why Mocks?
- **UI development** — Iterate on pages without needing a database, Identity, or email infrastructure
- **Predictable data** — Mocks generate consistent fake data via the [Bogus](https://github.com/bchavez/Bogus) library
- **Isolation** — Test UI behavior independently from backend logic

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
mocks/Server/Admin/ServicesMocks/
├── DevCoreApp.Admin.Services.Mocks.csproj   # References real services project + Bogus
├── UserAdmin/
│   └── UserProfileServiceMock.cs            # Mock for IUserProfileService
└── Email/
    └── EmailLogServiceMock.cs               # Mock for IEmailLogService
```

### Creating a New Mock
1. Create `{Entity}ServiceMock.cs` in the appropriate subfolder under `mocks/Server/Admin/ServicesMocks/`
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
