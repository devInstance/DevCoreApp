using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Admin.Services;
using DevInstance.DevCoreApp.Server.Admin.Services.ApiKeys;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Components;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Model.Grid;
using DevInstance.DevCoreApp.Shared.Model.ApiKeys;
using DevInstance.DevCoreApp.Shared.Model.Settings;
using DevInstance.WebServiceToolkit.Common.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.Admin;

public partial class ApiKeysPage
{
    private const string GridName = "AdminApiKeys";

    [Inject]
    private IApiKeyAdminService KeyService { get; set; } = default!;

    [Inject]
    private GridProfileService GridProfileService { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    private ModelList<ApiKeyItem>? KeyList { get; set; }

    public List<ColumnDescriptor<ApiKeyItem>> Columns { get; set; } = new()
    {
        new() { Label = "Name", Field = "name", ValueSelector = k => k.Name, Width = "20%" },
        new() { Label = "Prefix", Field = "prefix", ValueSelector = k => k.Prefix, IsSortable = false, Width = "12%" },
        new() { Label = "Scopes", Field = "scopes", ValueSelector = k => k.Scopes?.Count.ToString() ?? "All", IsSortable = false, Width = "15%" },
        new() { Label = "Created By", Field = "createdby", ValueSelector = k => k.CreatedByName ?? "", IsSortable = false, Width = "15%" },
        new() { Label = "Expires", Field = "expiresat", ValueSelector = k => k.ExpiresAt?.ToString("yyyy-MM-dd") ?? "Never", Width = "13%" },
        new() { Label = "Last Used", Field = "usedat", ValueSelector = k => k.LastUsedAt?.ToString("yyyy-MM-dd") ?? "Never", Width = "13%" },
        new() { Label = "Status", Field = "status", ValueSelector = k => k.IsRevoked ? "Revoked" : "Active", IsSortable = false, Width = "80px" },
        new() { Label = "Actions", Field = "actions", ValueSelector = k => k.Id, IsSortable = false, Width = "80px" },
    };

    private int pageCount = 10;
    private string SearchTerm { get; set; } = string.Empty;
    private string SortField { get; set; } = string.Empty;
    private bool IsAsc { get; set; } = true;

    // Create modal state
    private bool showCreateModal;
    private ApiKeyItem CreateItem { get; set; } = new();
    private string ScopesText { get; set; } = string.Empty;

    // Show key modal state
    private bool showKeyModal;
    private ApiKeyCreateResult? createdResult;
    private bool keyCopied;

    // Revoke modal state
    private ApiKeyItem? RevokingKey { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadGridProfile();
        await LoadKeys(0, SortField, IsAsc, null);
    }

    // ---------- Data Loading ----------

    private async Task LoadKeys(int page, string? sortField, bool? isAsc, string? search)
    {
        string[]? sortBy = !string.IsNullOrEmpty(sortField)
            ? new[] { (isAsc == false ? "-" : "") + sortField }
            : null;

        await Host.ServiceReadAsync(
            async () => await KeyService.GetKeysAsync(pageCount, page, sortBy, search),
            result => KeyList = result
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
        await LoadKeys(page, KeyList?.SortBy, KeyList?.IsAsc, KeyList?.Search);
    }

    public async Task OnSortAsync(HSortableHeaderSortArgs args)
    {
        SortField = args.SortBy;
        IsAsc = args.IsAscending;
        await SaveGridProfile();
        await LoadKeys(KeyList?.Page ?? 0, args.SortBy, args.IsAscending, KeyList?.Search);
    }

    public async Task OnColumnsChanged()
    {
        await SaveGridProfile();
    }

    public async Task OnSave(GridSettingsResult<ApiKeyItem> grid)
    {
        Columns = grid.Columns;
        var pageSizeChanged = pageCount != grid.PageSize;
        pageCount = grid.PageSize;
        await SaveGridProfile();

        if (pageSizeChanged)
            await LoadKeys(0, SortField, IsAsc, null);
    }

    public async Task OnSearch()
    {
        await LoadKeys(0, KeyList?.SortBy, KeyList?.IsAsc, SearchTerm);
    }

    public async Task OnClearSearch()
    {
        SearchTerm = string.Empty;
        await LoadKeys(0, KeyList?.SortBy, KeyList?.IsAsc, null);
    }

    // ---------- Create Modal ----------

    private void ShowCreateModal()
    {
        CreateItem = new ApiKeyItem();
        ScopesText = string.Empty;
        showCreateModal = true;
    }

    private void CloseCreateModal()
    {
        showCreateModal = false;
    }

    private async Task OnCreateKey()
    {
        CreateItem.Scopes = !string.IsNullOrWhiteSpace(ScopesText)
            ? ScopesText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : null;

        await Host.ServiceReadAsync(
            async () => await KeyService.CreateKeyAsync(CreateItem),
            result => createdResult = result
        );

        if (!Host.IsError && createdResult != null)
        {
            showCreateModal = false;
            showKeyModal = true;
            keyCopied = false;
        }
    }

    // ---------- Show Key Modal ----------

    private async Task CopyKeyToClipboard()
    {
        if (createdResult != null)
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", createdResult.PlainTextKey);
            keyCopied = true;
        }
    }

    private async Task CloseKeyModal()
    {
        showKeyModal = false;
        createdResult = null;
        await LoadKeys(0, SortField, IsAsc, null);
    }

    // ---------- Revoke Modal ----------

    private void ShowRevokeConfirmation(ApiKeyItem key)
    {
        RevokingKey = key;
    }

    private void CancelRevoke()
    {
        RevokingKey = null;
    }

    private async Task ConfirmRevoke()
    {
        if (RevokingKey == null) return;

        await Host.ServiceSubmitAsync(
            async () => await KeyService.RevokeKeyAsync(RevokingKey.Id)
        );

        RevokingKey = null;

        if (!Host.IsError)
        {
            await LoadKeys(KeyList?.Page ?? 0, KeyList?.SortBy, KeyList?.IsAsc, KeyList?.Search);
        }
    }
}
