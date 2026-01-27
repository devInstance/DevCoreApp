using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.WebService.Grid;
using DevInstance.DevCoreApp.Server.WebService.Services;
using DevInstance.DevCoreApp.Server.WebService.UI.Components;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.WebServiceToolkit.Common.Model;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Server.WebService.UI.Pages.Admin;

public partial class Users
{
    [Inject]
    private UserProfileService UserService { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    private ModelList<UserProfileItem>? UserList { get; set; }

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

    private int pageCount = 10;
    private string SearchTerm { get; set; } = string.Empty;
    private string SortField { get; set; } = string.Empty;
    private bool IsAsc { get; set; } = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadUsers(0, null, null, null);
    }

    private async Task LoadUsers(int page, string? sortField, bool? isAsc, string? search)
    {
        await Host.ServiceReadAsync(
            async () => await UserService.GetAllUsersAsync(pageCount, page, sortField, isAsc, search),
            (result) => UserList = result
        );
    }

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
