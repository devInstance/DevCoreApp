using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Admin.Services;
using DevInstance.DevCoreApp.Server.Admin.Services.Organizations;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Components;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Model.Grid;
using DevInstance.DevCoreApp.Shared.Model.Organizations;
using DevInstance.DevCoreApp.Shared.Model.Settings;
using DevInstance.WebServiceToolkit.Common.Model;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.Admin;

public partial class OrganizationTreePage
{
    private const string GridName = "AdminOrganizations";

    [Inject]
    private IOrganizationService OrgService { get; set; } = default!;

    [Inject]
    private GridProfileService GridProfileService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    // Grid view state
    private ModelList<OrganizationItem>? OrgList { get; set; }

    public List<ColumnDescriptor<OrganizationItem>> Columns { get; set; } = new()
    {
        new() { Label = "Name", Field = "name", ValueSelector = o => o.Name, Width = "24%" },
        new() { Label = "Code", Field = "code", ValueSelector = o => o.Code, Width = "12%" },
        new() { Label = "Type", Field = "type", ValueSelector = o => o.Type, Width = "12%" },
        new() { Label = "Path", Field = "path", ValueSelector = o => o.Path, Width = "20%" },
        new() { Label = "Level", Field = "level", ValueSelector = o => o.Level, Width = "8%" },
        new() { Label = "Status", Field = "status", ValueSelector = o => o.IsActive ? "Active" : "Inactive", IsSortable = false, Width = "10%" },
        new() { Label = "Actions", Field = "actions", ValueSelector = o => o.Id, IsSortable = false, Width = "140px" },
    };

    private int pageCount = 20;
    private string SearchTerm { get; set; } = string.Empty;
    private string SortField { get; set; } = string.Empty;
    private bool IsAsc { get; set; } = true;
    private string ActiveFilter { get; set; } = string.Empty;

    // Tree view state
    private bool ShowTreeView { get; set; } = true;
    private List<OrganizationItem>? TreeData { get; set; }
    private HashSet<string> ExpandedNodes { get; set; } = new();

    // Edit modal state
    private OrganizationItem? EditItem { get; set; }
    private string? EditPublicId { get; set; }
    private bool IsCreating { get; set; }
    private string? SelectedParentId { get; set; }

    // Move modal state
    private OrganizationItem? MoveItem { get; set; }
    private string? MoveTargetId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadGridProfile();
        await LoadTreeData();
    }

    // ---------- Data Loading ----------

    private async Task LoadTreeData()
    {
        await Host.ServiceReadAsync(
            async () => await OrgService.GetTreeAsync(),
            result =>
            {
                TreeData = result;
                // Auto-expand all nodes on first load
                if (ExpandedNodes.Count == 0 && TreeData != null)
                {
                    foreach (var org in TreeData)
                    {
                        ExpandedNodes.Add(org.Id);
                    }
                }
            }
        );
    }

    private async Task LoadOrgs(int page, string? sortField, bool? isAsc, string? search)
    {
        bool? isActive = null;
        if (bool.TryParse(ActiveFilter, out var val))
            isActive = val;

        await Host.ServiceReadAsync(
            async () => await OrgService.GetAllAsync(pageCount, page, sortField, isAsc, search, isActive),
            result => OrgList = result
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

    // ---------- View Toggle ----------

    private async Task ToggleTreeView()
    {
        ShowTreeView = !ShowTreeView;
        if (ShowTreeView)
        {
            await LoadTreeData();
        }
        else
        {
            await LoadOrgs(0, SortField, IsAsc, null);
        }
    }

    private void ToggleNode(string id)
    {
        if (!ExpandedNodes.Remove(id))
            ExpandedNodes.Add(id);
    }

    // ---------- Grid Events ----------

    public async Task OnPageChangedAsync(int page)
    {
        await LoadOrgs(page, OrgList?.SortBy, OrgList?.IsAsc, OrgList?.Search);
    }

    public async Task OnSortAsync(HSortableHeaderSortArgs args)
    {
        SortField = args.SortBy;
        IsAsc = args.IsAscending;
        await SaveGridProfile();
        await LoadOrgs(OrgList?.Page ?? 0, args.SortBy, args.IsAscending, OrgList?.Search);
    }

    public async Task OnColumnsChanged()
    {
        await SaveGridProfile();
    }

    public async Task OnSave(GridSettingsResult<OrganizationItem> grid)
    {
        Columns = grid.Columns;
        var pageSizeChanged = pageCount != grid.PageSize;
        pageCount = grid.PageSize;
        await SaveGridProfile();

        if (pageSizeChanged)
            await LoadOrgs(0, SortField, IsAsc, null);
    }

    public async Task OnSearch()
    {
        if (ShowTreeView)
        {
            await LoadTreeData();
        }
        else
        {
            await LoadOrgs(0, OrgList?.SortBy, OrgList?.IsAsc, SearchTerm);
        }
    }

    public async Task OnClearSearch()
    {
        SearchTerm = string.Empty;
        await LoadOrgs(0, OrgList?.SortBy, OrgList?.IsAsc, null);
    }

    private async Task OnApplyFilters()
    {
        if (ShowTreeView)
        {
            await LoadTreeData();
        }
        else
        {
            await LoadOrgs(0, OrgList?.SortBy, OrgList?.IsAsc, OrgList?.Search);
        }
    }

    private Task OnRowClick(OrganizationItem item)
    {
        ShowEditModal(item);
        return Task.CompletedTask;
    }

    // ---------- Create / Edit Modal ----------

    private void ShowCreateModal()
    {
        IsCreating = true;
        SelectedParentId = string.Empty;
        EditPublicId = null;
        EditItem = new OrganizationItem { Type = "Department" };
    }

    private void ShowCreateChildModal(OrganizationItem parent)
    {
        IsCreating = true;
        SelectedParentId = parent.Id;
        EditPublicId = null;
        EditItem = new OrganizationItem { Type = "Department" };
    }

    private void ShowEditModal(OrganizationItem org)
    {
        IsCreating = false;
        EditPublicId = org.Id;
        SelectedParentId = null;
        EditItem = new OrganizationItem
        {
            Id = org.Id,
            Name = org.Name,
            Code = org.Code,
            Type = org.Type,
            SortOrder = org.SortOrder,
            Settings = org.Settings,
            IsActive = org.IsActive,
            Path = org.Path,
            Level = org.Level,
            ParentId = org.ParentId
        };
    }

    private void CloseEditModal()
    {
        EditItem = null;
        EditPublicId = null;
    }

    private async Task OnSaveOrg()
    {
        if (EditItem == null) return;

        if (IsCreating)
        {
            await Host.ServiceSubmitAsync(
                async () => await OrgService.CreateAsync(EditItem, string.IsNullOrEmpty(SelectedParentId) ? null : SelectedParentId)
            );
        }
        else
        {
            await Host.ServiceSubmitAsync(
                async () => await OrgService.UpdateAsync(EditPublicId!, EditItem)
            );
        }

        if (!Host.IsError)
        {
            CloseEditModal();
            await RefreshCurrentView();
        }
    }

    // ---------- Toggle Active ----------

    private async Task OnToggleActive(OrganizationItem org)
    {
        await Host.ServiceSubmitAsync(
            async () => await OrgService.ToggleActiveAsync(org.Id)
        );

        if (!Host.IsError)
            await RefreshCurrentView();
    }

    // ---------- Move Modal ----------

    private void ShowMoveModal(OrganizationItem org)
    {
        MoveItem = org;
        MoveTargetId = string.Empty;
    }

    private void CloseMoveModal()
    {
        MoveItem = null;
        MoveTargetId = null;
    }

    private async Task OnConfirmMove()
    {
        if (MoveItem == null || string.IsNullOrEmpty(MoveTargetId)) return;

        await Host.ServiceSubmitAsync(
            async () => await OrgService.MoveAsync(MoveItem.Id, MoveTargetId)
        );

        if (!Host.IsError)
        {
            CloseMoveModal();
            await RefreshCurrentView();
        }
    }

    // ---------- Helpers ----------

    private async Task RefreshCurrentView()
    {
        if (ShowTreeView)
        {
            await LoadTreeData();
        }
        else
        {
            await LoadOrgs(OrgList?.Page ?? 0, OrgList?.SortBy, OrgList?.IsAsc, OrgList?.Search);
        }
    }
}
