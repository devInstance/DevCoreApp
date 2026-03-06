using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Admin.Services;
using DevInstance.DevCoreApp.Server.Admin.Services.UserAdmin;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Components;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Model.Grid;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.DevCoreApp.Shared.Model.Settings;
using DevInstance.WebServiceToolkit.Common.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

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
        new() { Label = "Actions", Field = "actions", ValueSelector = u => u.Id, IsSortable = false, Width = "5%" },
    };

    private string? UserToDelete { get; set; }
    private string? UserToDeleteName { get; set; }

    // Preview panel
    private UserProfileItem? SelectedUser { get; set; }
    private ElementReference PreviewPanelRef { get; set; }

    private int pageCount = 10;
    private string SearchTerm { get; set; } = string.Empty;
    private string SearchField { get; set; } = string.Empty;
    private string StatusFilter { get; set; } = string.Empty;
    private int UpdatedWithinDays { get; set; }
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
        var search = BuildSearchString();
        await LoadUsers(0, UserList?.SortBy, UserList?.IsAsc, search);
    }

    public async Task OnClearSearch()
    {
        SearchTerm = string.Empty;
        SearchField = string.Empty;
        StatusFilter = string.Empty;
        UpdatedWithinDays = 0;
        await LoadUsers(0, UserList?.SortBy, UserList?.IsAsc, null);
    }

    private string? BuildSearchString()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
            parts.Add(SearchTerm.Trim());

        if (!string.IsNullOrEmpty(SearchField))
            parts.Add($"field:{SearchField}");

        if (!string.IsNullOrEmpty(StatusFilter))
            parts.Add($"status:{StatusFilter}");

        if (UpdatedWithinDays > 0)
            parts.Add($"days:{UpdatedWithinDays}");

        return parts.Count > 0 ? string.Join(" | ", parts) : null;
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

    // ── Preview Panel ──

    private Task OnRowClick(UserProfileItem user)
    {
        SelectedUser = SelectedUser?.Id == user.Id ? null : user;
        return Task.CompletedTask;
    }

    private void ClosePreview()
    {
        SelectedUser = null;
    }

    private void OnPreviewKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            SelectedUser = null;
        }
    }

    private static string GetInitials(UserProfileItem user)
    {
        var first = !string.IsNullOrEmpty(user.FirstName) ? user.FirstName[0].ToString().ToUpper() : "";
        var last = !string.IsNullOrEmpty(user.LastName) ? user.LastName[0].ToString().ToUpper() : "";
        return first + last;
    }

    // ── Export Dialog ──

    private bool IsExportDialogVisible { get; set; }

    private void ShowExportDialog()
    {
        IsExportDialogVisible = true;
    }

    private void HideExportDialog()
    {
        IsExportDialogVisible = false;
    }

    private string[]? GetCurrentSortBy()
    {
        if (string.IsNullOrEmpty(SortField)) return null;
        return new[] { (IsAsc ? "" : "-") + SortField };
    }

    // ── Delete Confirmation ──

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

        // Close preview if we deleted the selected user
        if (SelectedUser?.Id == UserToDelete)
        {
            SelectedUser = null;
        }

        UserToDelete = null;
        UserToDeleteName = null;

        if (!Host.IsError)
        {
            await LoadUsers(UserList?.Page ?? 0, UserList?.SortBy, UserList?.IsAsc, UserList?.Search);
        }
    }
}
