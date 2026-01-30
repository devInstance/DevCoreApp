# CLAUDE.md — Project Conventions

## Architecture: Pages → Services → Repository

Pages must **never** access the database directly. All operations go through services via `IServiceExecutionHost`.

```
Page (Blazor) → Host.ServiceReadAsync / Host.ServiceSubmitAsync → Service → Repository
```

### Page Pattern
- Inject the service: `[Inject] private UserProfileService UserService { get; set; } = default!;`
- Receive the host as cascading parameter: `[CascadingParameter] private IServiceExecutionHost Host { get; set; } = default!;`
- Use `Host.ServiceReadAsync(async () => await Service.Method(), result => Property = result)` for reads
- Use `Host.ServiceSubmitAsync(async () => await Service.Method())` for writes
- Do **not** manage `IsSubmitting` / loading state manually — BlazorToolkit manages `Host.InProgress`
- Use `Host.InProgress`, `Host.IsError`, `Host.ErrorMessage` for UI state
- Do **not** inject `ApplicationDbContext`, `IUserStore`, `ITimeProvider`, or `IBackgroundWorker` into pages

### Service Pattern
- Inherit from `BaseService`
- Annotate with `[AppService]`
- Return `ServiceActionResult<T>` (use `ServiceActionResult<T>.OK(data)`)
- Access data via `Repository.GetXxxQuery(AuthorizationContext.CurrentProfile)`
- Create new records via `query.CreateNew()` + `entity.ToRecord(dto)` + `query.AddAsync(record)`
- Update existing records via `entity.ToRecord(dto)` + `query.UpdateAsync(record)`
- Background work (e.g., email) via `IBackgroundWorker.Submit()`

### Logging
- Create a local logger in the constructor: `log = logManager.CreateLogger(this);`
- Start each method with a trace scope: `using var l = log.TraceScope();`
- Use shorthand methods on the scope: `l.I("info message")`, `l.E("error message")`
- Do **not** use `ILogger` / `LogInformation` / `LogError` — use `IScopeLog` from `DevInstance.LogScope`

### ID Generation
- Use `IdGenerator.New()` from `DevInstance.BlazorToolkit.Utils` for generating unique public IDs and temporary values (e.g., temp passwords)

### DTO / Form Model Pattern
- DTOs (`{Entity}Item`) carry validation attributes (`[Required]`, `[EmailAddress]`, `[Phone]`, `[Display]`) directly — no separate `InputModel` classes in pages
- Pages bind forms directly to the DTO: `[SupplyParameterFromForm] private UserProfileItem Input { get; set; } = new();`
- Fields not part of the DTO (e.g., role selection during creation) live as separate page properties

### Naming Conventions
- **DTO / View Model:** `{Entity}Item` (e.g., `UserProfileItem`)
- **Service:** `{Entity}Service` (e.g., `UserProfileService`)
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

### Roles
Defined in `ApplicationRoles`: Owner, Admin, Manager, Employee, Client. Owner is the super-admin role and is typically excluded from user-assignable roles.
