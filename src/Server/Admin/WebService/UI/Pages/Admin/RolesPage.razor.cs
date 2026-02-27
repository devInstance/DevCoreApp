using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Admin.Services;
using DevInstance.DevCoreApp.Server.Admin.Services.Roles;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Components;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Model.Grid;
using DevInstance.DevCoreApp.Shared.Model.Roles;
using DevInstance.DevCoreApp.Shared.Model.Settings;
using DevInstance.WebServiceToolkit.Common.Model;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.Admin;

public partial class RolesPage
{
    private const string GridName = "AdminRoles";

    [Inject]
    private IRoleManagementService RoleService { get; set; } = default!;

    [Inject]
    private GridProfileService GridProfileService { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    private ModelList<RoleItem>? RoleList { get; set; }

    public List<ColumnDescriptor<RoleItem>> Columns { get; set; } = new()
    {
        new() { Label = "Name", Field = "name", ValueSelector = r => r.Name, Width = "30%" },
        new() { Label = "Description", Field = "description", ValueSelector = r => r.Description ?? string.Empty, Width = "35%" },
        new() { Label = "Permissions", Field = "permissions", ValueSelector = r => r.PermissionCount, IsSortable = false, Width = "15%" },
        new() { Label = "Actions", Field = "actions", ValueSelector = r => r.Id, IsSortable = false, Width = "120px" },
    };

    private int pageCount = 10;
    private string SearchTerm { get; set; } = string.Empty;
    private string SortField { get; set; } = string.Empty;
    private bool IsAsc { get; set; } = true;

    // Create modal state
    private bool showCreateModal;
    private RoleItem CreateItem { get; set; } = new();

    // Edit modal state
    private RoleItem? EditingRole { get; set; }
    private List<PermissionItem>? allPermissions;
    private HashSet<string> selectedPermissionKeys = new();

    // Delete modal state
    private RoleItem? DeletingRole { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadGridProfile();
        await LoadRoles(0, SortField, IsAsc, null);
    }

    // ---------- Data Loading ----------

    private async Task LoadRoles(int page, string? sortField, bool? isAsc, string? search)
    {
        string[]? sortBy = !string.IsNullOrEmpty(sortField)
            ? new[] { (isAsc == false ? "-" : "") + sortField }
            : null;

        await Host.ServiceReadAsync(
            async () => await RoleService.GetRolesAsync(pageCount, page, sortBy, search),
            result => RoleList = result
        );
    }

    private async Task LoadGridProfile()
    {
        await Host.ServiceReadAsync(
            async () => await GridProfileService.GetAsync(GridName),
            profile =>
            {
                if (profile != null)
                    ApplyGridProfile(profile);
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
                column.IsVisible = columnState.IsVisible;
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

    // ---------- Grid Events ----------

    public async Task OnPageChangedAsync(int page)
    {
        await LoadRoles(page, RoleList?.SortBy, RoleList?.IsAsc, RoleList?.Search);
    }

    public async Task OnSortAsync(HSortableHeaderSortArgs args)
    {
        SortField = args.SortBy;
        IsAsc = args.IsAscending;
        await SaveGridProfile();
        await LoadRoles(RoleList?.Page ?? 0, args.SortBy, args.IsAscending, RoleList?.Search);
    }

    public async Task OnColumnsChanged()
    {
        await SaveGridProfile();
    }

    public async Task OnSave(GridSettingsResult<RoleItem> grid)
    {
        Columns = grid.Columns;
        var pageSizeChanged = pageCount != grid.PageSize;
        pageCount = grid.PageSize;
        await SaveGridProfile();

        if (pageSizeChanged)
            await LoadRoles(0, SortField, IsAsc, null);
    }

    public async Task OnSearch()
    {
        await LoadRoles(0, RoleList?.SortBy, RoleList?.IsAsc, SearchTerm);
    }

    public async Task OnClearSearch()
    {
        SearchTerm = string.Empty;
        await LoadRoles(0, RoleList?.SortBy, RoleList?.IsAsc, null);
    }

    private Task OnRowClick(RoleItem item)
    {
        ShowEditModal(item);
        return Task.CompletedTask;
    }

    // ---------- Create Modal ----------

    private void ShowCreateModal()
    {
        CreateItem = new RoleItem();
        showCreateModal = true;
    }

    private void CloseCreateModal()
    {
        showCreateModal = false;
    }

    private async Task OnCreateRole()
    {
        await Host.ServiceSubmitAsync(
            async () => await RoleService.CreateRoleAsync(CreateItem)
        );

        if (!Host.IsError)
        {
            CloseCreateModal();
            await LoadRoles(0, SortField, IsAsc, null);
        }
    }

    // ---------- Edit + Permissions Modal ----------

    private async Task ShowEditModal(RoleItem role)
    {
        EditingRole = new RoleItem
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            PermissionCount = role.PermissionCount
        };

        allPermissions = null;
        selectedPermissionKeys = new HashSet<string>();

        // Load permissions catalog and current role's permission keys in parallel
        await Host.ServiceReadAsync(
            async () => await RoleService.GetAllPermissionsAsync(),
            result => allPermissions = result
        );

        await Host.ServiceReadAsync(
            async () => await RoleService.GetRolePermissionKeysAsync(role.Id),
            result => selectedPermissionKeys = new HashSet<string>(result)
        );
    }

    private void CloseEditModal()
    {
        EditingRole = null;
        allPermissions = null;
        selectedPermissionKeys = new HashSet<string>();
    }

    private async Task SaveRoleAndPermissions()
    {
        if (EditingRole == null) return;

        await Host.ServiceSubmitAsync(
            async () => await RoleService.UpdateRoleAsync(EditingRole.Id, EditingRole)
        );

        if (!Host.IsError && !EditingRole.IsSystemRole)
        {
            var request = new RolePermissionsRequest
            {
                PermissionKeys = selectedPermissionKeys.ToList()
            };

            await Host.ServiceSubmitAsync(
                async () => await RoleService.SetRolePermissionsAsync(EditingRole.Id, request)
            );
        }

        if (!Host.IsError)
        {
            CloseEditModal();
            await LoadRoles(RoleList?.Page ?? 0, RoleList?.SortBy, RoleList?.IsAsc, RoleList?.Search);
        }
    }

    // ---------- Delete Modal ----------

    private void ShowDeleteConfirmation(RoleItem role)
    {
        DeletingRole = role;
    }

    private void CancelDelete()
    {
        DeletingRole = null;
    }

    private async Task ConfirmDelete()
    {
        if (DeletingRole == null) return;

        await Host.ServiceSubmitAsync(
            async () => await RoleService.DeleteRoleAsync(DeletingRole.Id)
        );

        DeletingRole = null;

        if (!Host.IsError)
        {
            await LoadRoles(RoleList?.Page ?? 0, RoleList?.SortBy, RoleList?.IsAsc, RoleList?.Search);
        }
    }
}
