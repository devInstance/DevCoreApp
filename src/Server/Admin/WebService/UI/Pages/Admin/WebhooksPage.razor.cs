using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Admin.Services;
using DevInstance.DevCoreApp.Server.Admin.Services.Webhooks;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Components;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Model.Grid;
using DevInstance.DevCoreApp.Shared.Model.Settings;
using DevInstance.DevCoreApp.Shared.Model.Webhooks;
using DevInstance.WebServiceToolkit.Common.Model;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.Admin;

public partial class WebhooksPage
{
    private const string GridName = "AdminWebhooks";

    [Inject]
    private IWebhookAdminService WebhookService { get; set; } = default!;

    [Inject]
    private GridProfileService GridProfileService { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    private ModelList<WebhookSubscriptionItem>? SubscriptionList { get; set; }

    public List<ColumnDescriptor<WebhookSubscriptionItem>> Columns { get; set; } = new()
    {
        new() { Label = "Event Type", Field = "eventtype", ValueSelector = s => s.EventType, Width = "20%" },
        new() { Label = "URL", Field = "url", ValueSelector = s => s.Url, Width = "30%" },
        new() { Label = "Status", Field = "isactive", ValueSelector = s => s.IsActive ? "Active" : "Inactive", IsSortable = false, Width = "10%" },
        new() { Label = "Created By", Field = "createdby", ValueSelector = s => s.CreatedByName ?? "", IsSortable = false, Width = "15%" },
        new() { Label = "Created", Field = "createdate", ValueSelector = s => s.CreateDate.ToString("yyyy-MM-dd"), Width = "12%" },
        new() { Label = "Actions", Field = "actions", ValueSelector = s => s.Id, IsSortable = false, Width = "100px" },
    };

    private int pageCount = 10;
    private string SearchTerm { get; set; } = string.Empty;
    private string SortField { get; set; } = string.Empty;
    private bool IsAsc { get; set; } = true;

    // Create/Edit modal state
    private bool showModal;
    private bool isEditMode;
    private WebhookSubscriptionItem EditItem { get; set; } = new();
    private string? editingId;

    // Delete modal state
    private WebhookSubscriptionItem? DeletingSub { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadGridProfile();
        await LoadSubscriptions(0, SortField, IsAsc, null);
    }

    // ---------- Data Loading ----------

    private async Task LoadSubscriptions(int page, string? sortField, bool? isAsc, string? search)
    {
        string[]? sortBy = !string.IsNullOrEmpty(sortField)
            ? new[] { (isAsc == false ? "-" : "") + sortField }
            : null;

        await Host.ServiceReadAsync(
            async () => await WebhookService.GetSubscriptionsAsync(pageCount, page, sortBy, search),
            result => SubscriptionList = result
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
        await LoadSubscriptions(page, SubscriptionList?.SortBy, SubscriptionList?.IsAsc, SubscriptionList?.Search);
    }

    public async Task OnSortAsync(HSortableHeaderSortArgs args)
    {
        SortField = args.SortBy;
        IsAsc = args.IsAscending;
        await SaveGridProfile();
        await LoadSubscriptions(SubscriptionList?.Page ?? 0, args.SortBy, args.IsAscending, SubscriptionList?.Search);
    }

    public async Task OnColumnsChanged()
    {
        await SaveGridProfile();
    }

    public async Task OnSave(GridSettingsResult<WebhookSubscriptionItem> grid)
    {
        Columns = grid.Columns;
        var pageSizeChanged = pageCount != grid.PageSize;
        pageCount = grid.PageSize;
        await SaveGridProfile();

        if (pageSizeChanged)
            await LoadSubscriptions(0, SortField, IsAsc, null);
    }

    public async Task OnSearch()
    {
        await LoadSubscriptions(0, SubscriptionList?.SortBy, SubscriptionList?.IsAsc, SearchTerm);
    }

    public async Task OnClearSearch()
    {
        SearchTerm = string.Empty;
        await LoadSubscriptions(0, SubscriptionList?.SortBy, SubscriptionList?.IsAsc, null);
    }

    private Task OnRowClick(WebhookSubscriptionItem item)
    {
        ShowEditModal(item);
        return Task.CompletedTask;
    }

    // ---------- Create/Edit Modal ----------

    private void ShowCreateModal()
    {
        EditItem = new WebhookSubscriptionItem { IsActive = true };
        editingId = null;
        isEditMode = false;
        showModal = true;
    }

    private void ShowEditModal(WebhookSubscriptionItem sub)
    {
        EditItem = new WebhookSubscriptionItem
        {
            Id = sub.Id,
            EventType = sub.EventType,
            Url = sub.Url,
            IsActive = sub.IsActive
        };
        editingId = sub.Id;
        isEditMode = true;
        showModal = true;
    }

    private void CloseModal()
    {
        showModal = false;
        editingId = null;
    }

    private async Task OnSaveSubscription()
    {
        if (isEditMode && editingId != null)
        {
            await Host.ServiceSubmitAsync(
                async () => await WebhookService.UpdateSubscriptionAsync(editingId, EditItem)
            );
        }
        else
        {
            await Host.ServiceSubmitAsync(
                async () => await WebhookService.CreateSubscriptionAsync(EditItem)
            );
        }

        if (!Host.IsError)
        {
            CloseModal();
            await LoadSubscriptions(0, SortField, IsAsc, null);
        }
    }

    // ---------- Delete Modal ----------

    private void ShowDeleteConfirmation(WebhookSubscriptionItem sub)
    {
        DeletingSub = sub;
    }

    private void CancelDelete()
    {
        DeletingSub = null;
    }

    private async Task ConfirmDelete()
    {
        if (DeletingSub == null) return;

        await Host.ServiceSubmitAsync(
            async () => await WebhookService.DeleteSubscriptionAsync(DeletingSub.Id)
        );

        DeletingSub = null;

        if (!Host.IsError)
        {
            await LoadSubscriptions(SubscriptionList?.Page ?? 0, SubscriptionList?.SortBy, SubscriptionList?.IsAsc, SubscriptionList?.Search);
        }
    }
}
