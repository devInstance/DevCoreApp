# Contributing to DevCoreApp WebService

This document outlines the coding conventions and patterns used in this project.

## Project Structure

```
WebService/
├── Authentication/          # Identity and authorization
├── Background/              # Background services and workers
│   └── Requests/            # Background request models
├── Controllers/             # API controllers
├── Notifications/           # Email and notification services
├── Services/                # Business logic services
│   └── Admin/               # Admin-specific services
├── Tools/                   # Utility classes and extensions
├── UI/                      # Blazor UI components
│   ├── Account/             # Identity-related pages
│   │   └── Pages/
│   │       └── Admin/       # Admin account pages (e.g., Setup)
│   ├── Layout/              # Layout components
│   └── Pages/               # Application pages
│       └── Admin/           # Admin pages
└── ViewModel/               # View models for UI
    └── Admin/               # Admin-specific view models
```

## Services

### Creating a New Service

1. Create service class in `Services/` folder (or subfolder like `Services/Admin/`)
2. Add `[AppService]` attribute - this auto-registers the service as scoped
3. Return `ServiceActionResult<T>` from service methods

```csharp
using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.WebService.Tools;
using DevInstance.WebServiceToolkit.Common.Model;

namespace DevInstance.DevCoreApp.Server.WebService.Services.Admin;

[AppService]
public class UserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;

    public UserService(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<ServiceActionResult<ModelList<UserViewModel>>> GetAllUsersAsync(int? top, int? page)
    {
        // Business logic here...

        return ServiceActionResult<ModelList<UserViewModel>>.OK(new ModelList<UserViewModel>
        {
            Items = users.ToArray(),
            TotalCount = users.Count,
            Count = users.Count,
            Page = 0,
            PagesCount = 1
        });
    }
}
```

### BaseService

For services that need access to common dependencies (logging, time provider, query repository, authorization context), inherit from `BaseService`:

```csharp
[AppService]
public class MyService : BaseService
{
    public MyService(IScopeManager logManager,
                     ITimeProvider timeProvider,
                     IQueryRepository query,
                     IAuthorizationContext authorizationContext)
        : base(logManager, timeProvider, query, authorizationContext)
    {
    }
}
```

## View Models

Place view models in `ViewModel/` folder, organized by feature area:

```csharp
namespace DevInstance.DevCoreApp.Server.WebService.ViewModel.Admin;

public class UserViewModel
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    // ... properties

    // Computed properties are allowed
    public string FullName => $"{FirstName} {LastName}".Trim();
}
```

## Blazor Pages

### File Organization

Always separate Razor markup and C# code into two files:
- `PageName.razor` - Markup only
- `PageName.razor.cs` - Code-behind (partial class)

### Code-Behind Pattern

```csharp
using DevInstance.BlazorToolkit.Services;
using DevInstance.WebServiceToolkit.Common.Model;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Server.WebService.UI.Pages.Admin;

public partial class Users
{
    [Inject]
    private UserService UserService { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; }

    private ModelList<UserViewModel>? users;
    private int pageCount = 10;

    protected override async Task OnInitializedAsync()
    {
        await LoadUsers(pageCount, null, null, null, null);
    }

    public async Task OnPageChangedAsync(int page)
    {
        await LoadUsers(pageCount, page, null, null, null);
    }

    private async Task LoadUsers(int? top, int? page, string? sortBy, bool? sortDesc, string? filter)
    {
        await Host.ServiceReadAsync(
            async () => await UserService.GetAllUsersAsync(top, page),
            (result) => users = result
        );
    }
}
```

### Razor Markup Pattern

```razor
@page "/admin/users"
@attribute [Authorize(Roles = "Owner,Admin")]

<PageTitle>Users</PageTitle>

<h1>Users</h1>

@if (Host.InProgress)
{
    <p><em>Loading...</em></p>
}
else if (users != null)
{
    @if (!users.Items.Any())
    {
        <p>No users found.</p>
    }
    else
    {
        <table class="table table-striped">
            <!-- Table content -->
        </table>
        <ModelDataPager List="users" OnPageChanged="OnPageChangedAsync" />
    }
}
```

### IServiceExecutionHost

The `MainLayout` implements `IServiceExecutionHost` and provides:
- `InProgress` - Use for loading indicators
- `IsError` - Check if last operation failed
- `ErrorMessage` - Error message from last operation
- `ServiceReadAsync()` - Execute read operations with automatic state management

## Authorization

### Role-Based Access

Available roles are defined in `Authentication/ApplicationRoles.cs`:
- `Owner` - System owner (first registered user)
- `Admin` - Administrator
- `Manager` - Manager
- `Employee` - Employee
- `Client` - Client

### Protecting Pages

```razor
@attribute [Authorize(Roles = "Owner,Admin")]
```

### Protecting Navigation Items

```razor
<AuthorizeView Roles="Owner,Admin">
    <Authorized>
        <NavLink href="admin/users">Users</NavLink>
    </Authorized>
</AuthorizeView>
```

## Database Models

Database models are in `Database/Core/Models/`. Each model should:
- Inherit from `DatabaseObject`
- Include `PublicId` for external references
- Include `CreateDate` and `UpdateDate` timestamps

```csharp
public class UserProfile : DatabaseObject
{
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public Guid ApplicationUserId { get; set; }
    public UserStatus Status { get; set; }
}
```

## Migrations

Create migrations for both database providers:
- `Database/Postgres/Migrations/`
- `Database/SqlServer/Migrations/`

Migration naming convention: `YYYYMMDDHHMMSS_DescriptiveName.cs`

## Background Services

For async operations like email sending:

1. Create request model in `Background/Requests/`
2. Submit to `IBackgroundWorker`:

```csharp
_backgroundWorker.Submit(new BackgroundRequestItem
{
    RequestType = BackgroundRequestType.SendEmail,
    Content = emailRequest
});
```

## API Controllers

Use `WebServiceToolkit` patterns:

```csharp
[Route("api/user/profile")]
[ApiController]
public class UserProfileController : ControllerBase
{
    public UserProfileService Service { get; }

    public UserProfileController(UserProfileService service)
    {
        Service = service;
    }

    [Authorize]
    [HttpGet]
    public ActionResult<UserProfileItem> GetProfile()
    {
        return this.HandleWebRequest((WebHandler<UserProfileItem>)(() =>
        {
            return Ok(Service.Get());
        }));
    }
}
```

## Naming Conventions

- **Services**: `{Feature}Service` (e.g., `UserService`, `EmailSenderService`)
- **View Models**: `{Entity}ViewModel` (e.g., `UserViewModel`)
- **Pages**: Match the feature name (e.g., `Users.razor` for users list)
- **Controllers**: `{Entity}Controller` (e.g., `UserProfileController`)
