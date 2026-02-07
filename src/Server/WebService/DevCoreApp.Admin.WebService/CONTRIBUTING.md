# Contributing to DevCoreApp WebService

This document outlines the coding conventions and patterns used in this project.

## Project Structure

```
WebService/
├── Authentication/          # Identity and authorization
├── Background/              # Background services and workers
│   └── Requests/            # Background request models
├── Controllers/             # API controllers
├── Grid/                    # Grid components (ColumnDescriptor)
├── Notifications/           # Email and notification services
├── Services/                # Business logic services
├── Tools/                   # Utility classes and extensions
└── UI/                      # Blazor UI components
    ├── Account/             # Identity-related pages
    │   └── Pages/
    │       └── Admin/       # Admin account pages (e.g., Setup)
    ├── Components/          # Reusable UI components
    ├── Layout/              # Layout components
    └── Pages/               # Application pages
        └── Admin/           # Admin pages

Database/
├── Core/
│   ├── Data/
│   │   ├── Decorators/      # Model-to-ViewModel converters
│   │   └── Queries/         # Query interfaces
│   │       └── BasicsImplementation/  # Query implementations
│   └── Models/              # Database entities
├── Postgres/
│   └── Migrations/          # PostgreSQL migrations
└── SqlServer/
    └── Migrations/          # SQL Server migrations

Shared/
└── Model/                   # Shared view models (DTOs)
```

## Data Flow Architecture

The application follows a layered architecture for fetching and displaying data:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           UI Layer                                       │
│  ┌─────────────┐    ┌──────────────┐    ┌─────────────────────────────┐ │
│  │ Users.razor │───▶│ Users.razor.cs│───▶│ IServiceExecutionHost      │ │
│  │  (markup)   │    │ (code-behind) │    │ (MainLayout)               │ │
│  └─────────────┘    └──────────────┘    └─────────────────────────────┘ │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │ Host.ServiceReadAsync()
                                 ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         Service Layer                                    │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ UserProfileService : BaseService                                    │ │
│  │   - GetAllUsersAsync(top, page, sortField, isAsc, search)          │ │
│  │   - Returns ServiceActionResult<ModelList<UserProfileItem>>         │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │ Repository.GetUserProfilesQuery()
                                 ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                          Query Layer                                     │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ IUserProfilesQuery (interface)                                      │ │
│  │   - Search(), SortBy(), Paginate(), Select()                        │ │
│  │                                                                     │ │
│  │ CoreUserProfilesQuery (implementation)                              │ │
│  │   - Builds LINQ queries with fluent API                             │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │ EF Core
                                 ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        Database Layer                                    │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │ UserProfile (Database Model)                                        │ │
│  │   - Maps to database table                                          │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
```

### Data Conversion Flow

```
UserProfile (DB Model) ──▶ ToView() ──▶ UserProfileItem (DTO) ──▶ UI Display
     │                    Decorator                                    │
     │                                                                 │
     └─── Database/Core/Models/          Shared/Model/ ◀───────────────┘
```

## Complete Example: Users List Page

### 1. Razor Markup (`UI/Pages/Admin/Users.razor`)

```razor
@page "/admin/users"
@attribute [Authorize(Roles = "Owner,Admin")]

<PageTitle>Users</PageTitle>

<!-- Search and Action Bar -->
<div class="row mb-3">
    <div class="col col-md-6 col-lg-4 d-flex">
        <input class="form-control" placeholder="Search users..."
               @bind="SearchTerm" @bind:event="oninput"
               disabled="@Host.InProgress" />
        <button class="btn btn-primary ms-2" @onclick="OnSearch"
                disabled="@Host.InProgress">
            <i class="bi bi-search"></i>
        </button>
    </div>
    <div class="col col-md-6 col-lg-8">
        <div class="float-end">
            <a class="btn btn-primary ms-2" role="button" href="admin/users/new">
                <i class="bi bi-person-plus-fill me-1"></i>New User
            </a>
        </div>
    </div>
</div>

<!-- Loading State -->
@if (Host.InProgress)
{
    <div class="text-center">
        <div class="spinner-border" role="status">
            <span class="visually-hidden">Loading...</span>
        </div>
    </div>
}
else if (UserList != null)
{
    <!-- Search Indicator -->
    @if (!string.IsNullOrEmpty(UserList.Search))
    {
        <div class="alert alert-light">
            Searching for <strong>@SearchTerm</strong>
            <button type="button" class="btn-close ms-2" @onclick="OnClearSearch"></button>
        </div>
    }

    @if (!UserList.Items.Any())
    {
        <p>No users found.</p>
    }
    else
    {
        <!-- Data Table with Sortable Headers -->
        <table class="table table-striped table-hover">
            <thead>
                <tr>
                    @foreach (var col in Columns.Where(c => c.IsVisible))
                    {
                        @if (col.IsSortable)
                        {
                            <th scope="col">
                                <HSortableHeader Model="UserList" Label="@col.Label"
                                                 SortField="@col.Field" OnSort="OnSortAsync" />
                            </th>
                        }
                        else
                        {
                            <th scope="col">@col.Label</th>
                        }
                    }
                </tr>
            </thead>
            <tbody>
                @foreach (var row in UserList.Items)
                {
                    <tr>
                        @foreach (var col in Columns.Where(c => c.IsVisible))
                        {
                            <td>
                                @if (col.Template is not null)
                                {
                                    @col.Template(col.ValueSelector(row))
                                }
                                else
                                {
                                    @col.ValueSelector(row)
                                }
                            </td>
                        }
                    </tr>
                }
            </tbody>
        </table>

        <!-- Pagination -->
        <ModelDataPager List="UserList" OnPageChanged="OnPageChangedAsync" />
    }
}

<!-- Grid Settings Panel -->
<GridSettings Columns="Columns" OnSave="OnSave" TItem="UserProfileItem" />
```

### 2. Code-Behind (`UI/Pages/Admin/Users.razor.cs`)

```csharp
using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Admin.WebService.Grid;
using DevInstance.DevCoreApp.Server.Admin.WebService.Services;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Components;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.WebServiceToolkit.Common.Model;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.Admin;

public partial class Users
{
    [Inject]
    private UserProfileService UserService { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    // Data bound to the grid
    private ModelList<UserProfileItem>? UserList { get; set; }

    // Column definitions with sorting and visibility
    public List<ColumnDescriptor<UserProfileItem>> Columns { get; set; } = new()
    {
        new() { Label = "Email", Field = "email", ValueSelector = u => u.Email },
        new() { Label = "First Name", Field = "firstname", ValueSelector = u => u.FirstName },
        new() { Label = "Middle Name", Field = "middlename", ValueSelector = u => u.MiddleName, IsVisible = false },
        new() { Label = "Last Name", Field = "lastname", ValueSelector = u => u.LastName },
        new() { Label = "Phone", Field = "phone", ValueSelector = u => u.PhoneNumber },
        new() { Label = "Roles", Field = "roles", ValueSelector = u => u.Roles, IsSortable = false },
        new() { Label = "Status", Field = "status", ValueSelector = u => u.Status.ToString() },
    };

    // Grid state
    private int pageCount = 10;
    private string SearchTerm { get; set; } = string.Empty;
    private string SortField { get; set; } = string.Empty;
    private bool IsAsc { get; set; } = true;

    // Initial load
    protected override async Task OnInitializedAsync()
    {
        await LoadUsers(0, null, null, null);
    }

    // Central data loading method
    private async Task LoadUsers(int page, string? sortField, bool? isAsc, string? search)
    {
        await Host.ServiceReadAsync(
            async () => await UserService.GetAllUsersAsync(pageCount, page, sortField, isAsc, search),
            (result) => UserList = result
        );
    }

    // Event handlers
    public async Task OnPageChangedAsync(int page)
    {
        await LoadUsers(page, UserList?.SortBy, UserList?.IsAsc, UserList?.Search);
    }

    public async Task OnSave(GridSettingsResult<UserProfileItem> grid)
    {
        Columns = grid.Columns;
        if (pageCount != grid.PageSize)
        {
            pageCount = grid.PageSize;
            await LoadUsers(0, SortField, IsAsc, null);
        }
    }

    public async Task OnSearch()
    {
        await LoadUsers(0, UserList?.SortBy, UserList?.IsAsc, SearchTerm);
    }

    public async Task OnClearSearch()
    {
        SearchTerm = string.Empty;
        await LoadUsers(0, UserList?.SortBy, UserList?.IsAsc, null);
    }

    public async Task OnSortAsync(HSortableHeaderSortArgs args)
    {
        SortField = args.SortBy;
        IsAsc = args.IsAscending;
        await LoadUsers(UserList?.Page ?? 0, args.SortBy, args.IsAscending, UserList?.Search);
    }
}
```

### 3. Service (`Services/UserProfileService.cs`)

```csharp
[AppService]
public class UserProfileService : BaseService
{
    public UserManager<ApplicationUser> UserManager { get; }

    public UserProfileService(IScopeManager logManager,
                              ITimeProvider timeProvider,
                              IQueryRepository query,
                              IAuthorizationContext authorizationContext,
                              UserManager<ApplicationUser> userManager)
        : base(logManager, timeProvider, query, authorizationContext)
    {
        UserManager = userManager;
    }

    public async Task<ServiceActionResult<ModelList<UserProfileItem>>> GetAllUsersAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null, string? search = null)
    {
        // Get query from repository
        var profilesQuery = Repository.GetUserProfilesQuery(AuthorizationContext.CurrentProfile);

        // Apply search filter
        if (!string.IsNullOrEmpty(search))
        {
            profilesQuery = profilesQuery.Search(search);
        }

        // Apply sorting
        if (!string.IsNullOrEmpty(sortField))
        {
            profilesQuery = profilesQuery.SortBy(sortField, isAsc ?? true);
        }

        // Get total count before pagination
        var totalCount = await profilesQuery.Clone().Select().CountAsync();

        // Apply pagination and execute
        var userProfiles = await profilesQuery.Paginate(top, page).Select().ToListAsync();

        // Convert to view models
        var users = new List<UserProfileItem>();
        foreach (var profile in userProfiles)
        {
            var appUser = await UserManager.FindByIdAsync(profile.ApplicationUserId.ToString());
            if (appUser != null)
            {
                var roles = await UserManager.GetRolesAsync(appUser);
                users.Add(profile.ToView(appUser, roles));  // Decorator method
            }
        }

        // Create paginated result
        var modelList = ModelListResult.CreateList(users.ToArray(), totalCount, top, page, sortField, isAsc, search);
        return ServiceActionResult<ModelList<UserProfileItem>>.OK(modelList);
    }
}
```

### 4. Query Interface (`Database/Core/Data/Queries/IUserProfilesQuery.cs`)

```csharp
public interface IUserProfilesQuery : IModelQuery<UserProfile, IUserProfilesQuery>,
        IQSearchable<IUserProfilesQuery>,
        IQPageable<IUserProfilesQuery>,
        IQSortable<IUserProfilesQuery>
{
    IQueryable<UserProfile> Select();
    IUserProfilesQuery ByLastName(string lastName);
    IUserProfilesQuery ByApplicationUserId(Guid id);
}
```

### 5. Query Implementation (`Database/Core/Data/Queries/BasicsImplementation/CoreUserProfilesQuery.cs`)

```csharp
public class CoreUserProfilesQuery : CoreBaseQuery, IUserProfilesQuery
{
    private IQueryable<UserProfile> currentQuery;

    public CoreUserProfilesQuery(IScopeManager logManager,
                                 ITimeProvider timeProvider,
                                 ApplicationDbContext dB,
                                 UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
        currentQuery = from ts in dB.UserProfiles select ts;
    }

    public IUserProfilesQuery Search(string search)
    {
        currentQuery = from profile in currentQuery
                       where profile.FirstName.IndexOf(search) >= 0 ||
                             profile.LastName.IndexOf(search) >= 0 ||
                             profile.Email.IndexOf(search) >= 0 ||
                             profile.PhoneNumber.IndexOf(search) >= 0 ||
                             profile.MiddleName.IndexOf(search) >= 0
                       select profile;
        return this;
    }

    public IUserProfilesQuery SortBy(string column, bool isAsc)
    {
        // Example for one column - implement for each sortable field
        if (string.Compare(column, "Email", true) == 0)
        {
            currentQuery = isAsc
                ? currentQuery.OrderBy(ts => ts.Email)
                : currentQuery.OrderByDescending(ts => ts.Email);
        }
        // ... other columns
        return this;
    }

    public IUserProfilesQuery Clone()
    {
        return new CoreUserProfilesQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public IQueryable<UserProfile> Select() => currentQuery;
}
```

### 6. Decorator (`Database/Core/Data/Decorators/UserProfileDecorators.cs`)

```csharp
public static class UserProfileDecorators
{
    public static UserProfileItem ToView(this UserProfile profile,
        ApplicationUser? appUser = null, IList<string>? roles = null)
    {
        return new UserProfileItem
        {
            Id = profile.PublicId,
            Email = appUser?.Email ?? string.Empty,
            FirstName = profile.FirstName ?? string.Empty,
            MiddleName = profile.MiddleName ?? string.Empty,
            LastName = profile.LastName ?? string.Empty,
            PhoneNumber = profile.PhoneNumber ?? string.Empty,
            Roles = roles != null ? string.Join(", ", roles) : string.Empty,
            Status = profile.Status.ToString(),
            CreateDate = profile.CreateDate,
            UpdateDate = profile.UpdateDate
        };
    }

    public static UserProfile ToRecord(this UserProfile profile, UserProfileItem newProfile)
    {
        profile.FirstName = newProfile.FirstName;
        profile.MiddleName = newProfile.MiddleName;
        profile.LastName = newProfile.LastName;
        profile.PhoneNumber = newProfile.PhoneNumber;
        return profile;
    }
}
```

### 7. Database Model (`Database/Core/Models/UserProfile.cs`)

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

### 8. Shared DTO (`Shared/Model/UserProfileItem.cs`)

```csharp
public class UserProfileItem : ModelItem
{
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public string Roles { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }

    // Computed property
    public string FullName => string.Join(" ", new[] { FirstName, MiddleName, LastName }
        .Where(s => !string.IsNullOrWhiteSpace(s)));
}
```

## Grid Components

### ColumnDescriptor (`Grid/ColumnDescriptor.cs`)

```csharp
public class ColumnDescriptor<TItem>
{
    public string Field { get; init; } = default!;           // Field name for sorting
    public string Label { get; init; } = default!;           // Display label
    public Func<TItem, object?> ValueSelector { get; init; } // Value extractor
    public RenderFragment<object?>? Template { get; init; }  // Custom rendering
    public bool IsVisible { get; set; } = true;              // Show/hide column
    public bool IsDragable { get; set; } = false;            // Drag state
    public bool IsSortable { get; set; } = true;             // Enable sorting
    public string Class { get; set; } = string.Empty;        // CSS class
}
```

### HSortableHeader (`UI/Components/HSortableHeader.razor`)

Sortable column header component with ascending/descending toggle.

### GridSettings (`UI/Components/GridSettings.razor`)

Offcanvas panel for column visibility, ordering, and page size settings.

## Services

### Creating a New Service

1. Create service class in `Services/` folder
2. Inherit from `BaseService` for repository access
3. Add `[AppService]` attribute for auto-registration
4. Return `ServiceActionResult<T>` from methods

### BaseService Dependencies

- `IScopeManager logManager` - Logging
- `ITimeProvider timeProvider` - Current time
- `IQueryRepository query` - Database queries
- `IAuthorizationContext authorizationContext` - Current user

## IServiceExecutionHost

The `MainLayout` implements `IServiceExecutionHost` and provides:
- `InProgress` - Loading state indicator
- `IsError` - Error state flag
- `ErrorMessage` - Error details
- `ServiceReadAsync()` - Execute read operations with automatic state management

## Authorization

### Role-Based Access

Available roles in `Authentication/ApplicationRoles.cs`:
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

## Migrations

Create migrations for both database providers:
- `Database/Postgres/Migrations/`
- `Database/SqlServer/Migrations/`

Naming convention: `YYYYMMDDHHMMSS_DescriptiveName.cs`

## Background Services

For async operations like email sending:

```csharp
_backgroundWorker.Submit(new BackgroundRequestItem
{
    RequestType = BackgroundRequestType.SendEmail,
    Content = emailRequest
});
```

## API Controllers

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

- **Services**: `{Feature}Service` (e.g., `UserProfileService`)
- **DTOs**: `{Entity}Item` (e.g., `UserProfileItem`)
- **Pages**: Match the feature (e.g., `Users.razor`)
- **Controllers**: `{Entity}Controller` (e.g., `UserProfileController`)
- **Queries**: `I{Entity}Query` / `Core{Entity}Query`
- **Decorators**: `{Entity}Decorators` (static class)
