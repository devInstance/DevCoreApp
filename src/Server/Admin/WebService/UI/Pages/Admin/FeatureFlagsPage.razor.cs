using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Admin.Services;
using DevInstance.DevCoreApp.Server.Admin.Services.FeatureFlags;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Components;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Model.Grid;
using DevInstance.DevCoreApp.Shared.Model.FeatureFlags;
using DevInstance.DevCoreApp.Shared.Model.Settings;
using DevInstance.WebServiceToolkit.Common.Model;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.Admin;

public partial class FeatureFlagsPage
{
    private const string GridName = "AdminFeatureFlags";

    [Inject]
    private IFeatureFlagAdminService FlagService { get; set; } = default!;

    [Inject]
    private GridProfileService GridProfileService { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    private ModelList<FeatureFlagItem>? FlagList { get; set; }

    public List<ColumnDescriptor<FeatureFlagItem>> Columns { get; set; } = new()
    {
        new() { Label = "Name", Field = "name", ValueSelector = f => f.Name, Width = "25%" },
        new() { Label = "Description", Field = "description", ValueSelector = f => f.Description ?? string.Empty, IsSortable = false, Width = "25%" },
        new() { Label = "Scope", Field = "scope", ValueSelector = f => f.OrganizationName ?? "Global", IsSortable = false, Width = "15%" },
        new() { Label = "Enabled", Field = "isenabled", ValueSelector = f => f.IsEnabled, Width = "10%" },
        new() { Label = "Rollout", Field = "rollout", ValueSelector = f => f.RolloutPercentage?.ToString() ?? "", IsSortable = false, Width = "10%" },
        new() { Label = "Actions", Field = "actions", ValueSelector = f => f.Id, IsSortable = false, Width = "120px" },
    };

    private int pageCount = 10;
    private string SearchTerm { get; set; } = string.Empty;
    private string SortField { get; set; } = string.Empty;
    private bool IsAsc { get; set; } = true;

    // Create/Edit modal state
    private bool showModal;
    private bool isEditMode;
    private FeatureFlagItem EditItem { get; set; } = new();
    private string? editingId;
    private string AllowedUsersText { get; set; } = string.Empty;

    // Delete modal state
    private FeatureFlagItem? DeletingFlag { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadGridProfile();
        await LoadFlags(0, SortField, IsAsc, null);
    }

    // ---------- Data Loading ----------

    private async Task LoadFlags(int page, string? sortField, bool? isAsc, string? search)
    {
        string[]? sortBy = !string.IsNullOrEmpty(sortField)
            ? new[] { (isAsc == false ? "-" : "") + sortField }
            : null;

        await Host.ServiceReadAsync(
            async () => await FlagService.GetFlagsAsync(pageCount, page, sortBy, search),
            result => FlagList = result
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
        await LoadFlags(page, FlagList?.SortBy, FlagList?.IsAsc, FlagList?.Search);
    }

    public async Task OnSortAsync(HSortableHeaderSortArgs args)
    {
        SortField = args.SortBy;
        IsAsc = args.IsAscending;
        await SaveGridProfile();
        await LoadFlags(FlagList?.Page ?? 0, args.SortBy, args.IsAscending, FlagList?.Search);
    }

    public async Task OnColumnsChanged()
    {
        await SaveGridProfile();
    }

    public async Task OnSave(GridSettingsResult<FeatureFlagItem> grid)
    {
        Columns = grid.Columns;
        var pageSizeChanged = pageCount != grid.PageSize;
        pageCount = grid.PageSize;
        await SaveGridProfile();

        if (pageSizeChanged)
            await LoadFlags(0, SortField, IsAsc, null);
    }

    public async Task OnSearch()
    {
        await LoadFlags(0, FlagList?.SortBy, FlagList?.IsAsc, SearchTerm);
    }

    public async Task OnClearSearch()
    {
        SearchTerm = string.Empty;
        await LoadFlags(0, FlagList?.SortBy, FlagList?.IsAsc, null);
    }

    private Task OnRowClick(FeatureFlagItem item)
    {
        ShowEditModal(item);
        return Task.CompletedTask;
    }

    // ---------- Create/Edit Modal ----------

    private void ShowCreateModal()
    {
        EditItem = new FeatureFlagItem();
        AllowedUsersText = string.Empty;
        editingId = null;
        isEditMode = false;
        showModal = true;
    }

    private void ShowEditModal(FeatureFlagItem flag)
    {
        EditItem = new FeatureFlagItem
        {
            Id = flag.Id,
            Name = flag.Name,
            Description = flag.Description,
            IsEnabled = flag.IsEnabled,
            OrganizationId = flag.OrganizationId,
            RolloutPercentage = flag.RolloutPercentage,
            AllowedUsers = flag.AllowedUsers != null ? new List<string>(flag.AllowedUsers) : null,
            OrganizationName = flag.OrganizationName
        };
        AllowedUsersText = flag.AllowedUsers != null ? string.Join(", ", flag.AllowedUsers) : string.Empty;
        editingId = flag.Id;
        isEditMode = true;
        showModal = true;
    }

    private void CloseModal()
    {
        showModal = false;
        editingId = null;
    }

    private async Task OnSaveFlag()
    {
        // Parse allowed users from comma-separated text
        EditItem.AllowedUsers = !string.IsNullOrWhiteSpace(AllowedUsersText)
            ? AllowedUsersText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : null;

        if (isEditMode && editingId != null)
        {
            await Host.ServiceSubmitAsync(
                async () => await FlagService.UpdateFlagAsync(editingId, EditItem)
            );
        }
        else
        {
            await Host.ServiceSubmitAsync(
                async () => await FlagService.CreateFlagAsync(EditItem)
            );
        }

        if (!Host.IsError)
        {
            CloseModal();
            await LoadFlags(0, SortField, IsAsc, null);
        }
    }

    // ---------- Delete Modal ----------

    private void ShowDeleteConfirmation(FeatureFlagItem flag)
    {
        DeletingFlag = flag;
    }

    private void CancelDelete()
    {
        DeletingFlag = null;
    }

    private async Task ConfirmDelete()
    {
        if (DeletingFlag == null) return;

        await Host.ServiceSubmitAsync(
            async () => await FlagService.DeleteFlagAsync(DeletingFlag.Id)
        );

        DeletingFlag = null;

        if (!Host.IsError)
        {
            await LoadFlags(FlagList?.Page ?? 0, FlagList?.SortBy, FlagList?.IsAsc, FlagList?.Search);
        }
    }
}
