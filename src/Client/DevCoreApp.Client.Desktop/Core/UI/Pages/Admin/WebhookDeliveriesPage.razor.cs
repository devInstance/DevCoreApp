using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Client.Services.Core.GridProfiles;
using DevInstance.DevCoreApp.Client.Services.Core.Webhooks;
using DevInstance.DevCoreApp.Client.Desktop.Core.UI.Components;
using DevInstance.BlazorToolkit.Components.DataGrid;
using DevInstance.DevCoreApp.Client.Desktop.Core.UI.Model.Grid;
using DevInstance.DevCoreApp.Shared.Model.Core.Settings;
using DevInstance.DevCoreApp.Shared.Model.Core.Webhooks;
using DevInstance.WebServiceToolkit.Common.Model;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Client.Desktop.Core.UI.Pages.Admin;

public partial class WebhookDeliveriesPage
{
    private const string GridName = "AdminWebhookDeliveries";

    [Parameter]
    public string? SubscriptionId { get; set; }

    [Inject]
    private IWebhookAdminService WebhookService { get; set; } = default!;

    [Inject]
    private IGridProfileService GridProfileService { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    private PagedList<WebhookDeliveryItem>? DeliveryList { get; set; }

    public List<ColumnDescriptor<WebhookDeliveryItem>> Columns { get; set; } = new()
    {
        new() { Label = "Event Type", Field = "eventtype", ValueSelector = d => d.EventType, Width = "18%" },
        new() { Label = "URL", Field = "url", ValueSelector = d => d.Url, IsSortable = false, Width = "25%" },
        new() { Label = "Status", Field = "status", ValueSelector = d => d.Status.ToString(), Width = "10%" },
        new() { Label = "HTTP", Field = "responsecode", ValueSelector = d => d.ResponseStatusCode?.ToString() ?? "", IsSortable = false, Width = "8%" },
        new() { Label = "Attempts", Field = "attempts", ValueSelector = d => d.AttemptCount.ToString(), IsSortable = false, Width = "10%" },
        new() { Label = "Created", Field = "createdate", ValueSelector = d => LocalClock.Format(d.CreateDate, "yyyy-MM-dd HH:mm"), Width = "17%" },
    };

    private int pageCount = 20;
    private string SortField { get; set; } = string.Empty;
    private bool IsAsc { get; set; } = true;

    // Detail modal
    private WebhookDeliveryItem? SelectedDelivery { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadGridProfile();
        await LoadDeliveries(0, SortField, IsAsc);
    }

    // ---------- Data Loading ----------

    private async Task LoadDeliveries(int page, string? sortField, bool? isAsc)
    {
        string[]? sortBy = !string.IsNullOrEmpty(sortField)
            ? new[] { (isAsc == false ? "-" : "") + sortField }
            : null;

        await Host.ServiceReadAsync(
            async () => await WebhookService.GetDeliveriesAsync(pageCount, page, SubscriptionId, sortBy),
            result => DeliveryList = result
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

        Columns = Columns.ApplyProfile(profile);
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
            Columns = Columns.ToColumnStates()
        };

        // Not through Host: it would flip Host.InProgress, which the grid shows as loading rows on
        // every resize / move / sort. A layout preference that fails to save is not worth an error banner.
        await GridProfileService.SaveAsync(profileItem);
    }

    // ---------- Grid Events ----------

    public async Task OnPageChangedAsync(int page)
    {
        await LoadDeliveries(page, DeliveryList?.SortField(), DeliveryList?.IsAscending());
    }

    public async Task OnSortAsync(HSortableHeaderSortArgs args)
    {
        SortField = args.SortBy;
        IsAsc = args.IsAscending;
        await SaveGridProfile();
        await LoadDeliveries(DeliveryList?.Page ?? 0, args.SortBy, args.IsAscending);
    }

    public async Task OnColumnsChanged()
    {
        await SaveGridProfile();
    }

    public async Task OnSave(GridSettingsResult<WebhookDeliveryItem> grid)
    {
        Columns = grid.Columns;
        var pageSizeChanged = pageCount != grid.PageSize;
        pageCount = grid.PageSize;
        await SaveGridProfile();

        if (pageSizeChanged)
            await LoadDeliveries(0, SortField, IsAsc);
    }

    // ---------- Detail Modal ----------

    private Task OnRowClick(WebhookDeliveryItem item)
    {
        SelectedDelivery = item;
        return Task.CompletedTask;
    }

    private void CloseDetail()
    {
        SelectedDelivery = null;
    }
}
