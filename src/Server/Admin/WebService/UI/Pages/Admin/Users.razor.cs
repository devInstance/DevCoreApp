using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Admin.Services;
using DevInstance.DevCoreApp.Server.Admin.Services.UserAdmin;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Components;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Model.Grid;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.WebServiceToolkit.Common.Model;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.Admin;

public partial class Users
{
    private const string GridName = "AdminUsers";

    [Inject]
    private IUserProfileService UserService { get; set; } = default!;

    [Inject]
    private GridProfileService GridProfileService { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    private ModelList<UserProfileItem>? UserList { get; set; }

    public List<ColumnDescriptor<UserProfileItem>> Columns { get; set; } = new()
    {
        new() { Label = "Email", Field = "email", ValueSelector = u => u.Email, Width = "20%" },
        new() { Label = "First Name", Field = "firstname", ValueSelector = u => u.FirstName, Width = "14%" },
        new() { Label = "Middle Name", Field = "middlename", ValueSelector = u => u.MiddleName, IsVisible = false, Width = "14%" },
        new() { Label = "Last Name", Field = "lastname", ValueSelector = u => u.LastName, Width = "14%" },
        new() { Label = "Phone", Field = "phone", ValueSelector = u => u.PhoneNumber, Width = "14%" },
        new() { Label = "Roles", Field = "roles", ValueSelector = u => u.Roles, IsSortable = false, Width = "10%" },
        new() { Label = "Status", Field = "status", ValueSelector = u => u.Status.ToString(), Width = "10%" },
        new() { Label = "Actions", Field = "actions", ValueSelector = u => u.Id, IsSortable = false, Width = "100px" },
    };

    private string? UserToDelete { get; set; }
    private string? UserToDeleteName { get; set; }

    private int pageCount = 10;
    private string SearchTerm { get; set; } = string.Empty;
    private string SortField { get; set; } = string.Empty;
    private bool IsAsc { get; set; } = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadGridProfile();
        await LoadUsers(0, SortField, IsAsc, null);
    }

    private async Task LoadGridProfile()
    {
        await Host.ServiceReadAsync(
            async () => await GridProfileService.GetAsync(GridName),
            (profile) =>
            {
                if (profile != null)
                {
                    ApplyGridProfile(profile);
                }
            }
        );
    }

    private void ApplyGridProfile(GridProfileItem profile)
    {
        pageCount = profile.PageSize;
        SortField = profile.SortField ?? string.Empty;
        IsAsc = profile.IsAsc;

        foreach (var columnState in profile.Columns)
        {
            var column = Columns.FirstOrDefault(c => c.Field == columnState.Field);
            if (column != null)
            {
                column.IsVisible = columnState.IsVisible;
            }
        }

        if (profile.Columns.Count > 0)
        {
            Columns = Columns
                .OrderBy(c => profile.Columns.FindIndex(cs => cs.Field == c.Field) is var idx && idx >= 0 ? idx : int.MaxValue)
                .ToList();
        }
    }

    private async Task SaveGridProfile()
    {
        var profileItem = new GridProfileItem
        {
            GridName = GridName,
            ProfileName = "Default",
            PageSize = pageCount,
            SortField = string.IsNullOrEmpty(SortField) ? null : SortField,
            IsAsc = IsAsc,
            Columns = Columns.Select((c, index) => new GridColumnState
            {
                Field = c.Field,
                IsVisible = c.IsVisible,
                Order = index
            }).ToList()
        };

        await Host.ServiceSubmitAsync(
            async () => await GridProfileService.SaveAsync(profileItem)
        );
    }

    private async Task LoadUsers(int page, string? sortField, bool? isAsc, string? search)
    {
        string[] sortBy = !string.IsNullOrEmpty(sortField)
            ? new[] { (isAsc == false ? "-" : "") + sortField }
            : null;

        await Host.ServiceReadAsync(
            async () => await UserService.GetListAsync(pageCount, page, sortBy, search),
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
        var pageSizeChanged = pageCount != grid.PageSize;
        pageCount = grid.PageSize;

        await SaveGridProfile();

        if (pageSizeChanged)
        {
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

    public async Task OnColumnsChanged()
    {
        await SaveGridProfile();
    }

    public async Task OnSortAsync(HSortableHeaderSortArgs args)
    {
        SortField = args.SortBy;
        IsAsc = args.IsAscending;
        await SaveGridProfile();
        await LoadUsers(UserList?.Page ?? 0, args.SortBy, args.IsAscending, UserList?.Search);
    }

    private void ShowDeleteConfirmation(UserProfileItem user)
    {
        UserToDelete = user.Id;
        UserToDeleteName = user.FullName;
    }

    private void CancelDelete()
    {
        UserToDelete = null;
        UserToDeleteName = null;
    }

    private async Task ConfirmDelete()
    {
        if (string.IsNullOrEmpty(UserToDelete)) return;

        await Host.ServiceSubmitAsync(
            async () => await UserService.DeleteUserAsync(UserToDelete)
        );

        UserToDelete = null;
        UserToDeleteName = null;

        if (!Host.IsError)
        {
            await LoadUsers(UserList?.Page ?? 0, UserList?.SortBy, UserList?.IsAsc, UserList?.Search);
        }
    }
}
