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

public partial class WebhookDeliveriesPage
{
    private const string GridName = "AdminWebhookDeliveries";

    [Parameter]
    public string? SubscriptionId { get; set; }

    [Inject]
    private IWebhookAdminService WebhookService { get; set; } = default!;

    [Inject]
    private GridProfileService GridProfileService { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    private ModelList<WebhookDeliveryItem>? DeliveryList { get; set; }

    public List<ColumnDescriptor<WebhookDeliveryItem>> Columns { get; set; } = new()
    {
        new() { Label = "Event Type", Field = "eventtype", ValueSelector = d => d.EventType, Width = "18%" },
        new() { Label = "URL", Field = "url", ValueSelector = d => d.Url, IsSortable = false, Width = "25%" },
        new() { Label = "Status", Field = "status", ValueSelector = d => d.Status.ToString(), Width = "10%" },
        new() { Label = "HTTP", Field = "responsecode", ValueSelector = d => d.ResponseStatusCode?.ToString() ?? "", IsSortable = false, Width = "8%" },
        new() { Label = "Attempts", Field = "attempts", ValueSelector = d => d.AttemptCount.ToString(), IsSortable = false, Width = "10%" },
        new() { Label = "Created", Field = "createdate", ValueSelector = d => d.CreateDate.ToString("yyyy-MM-dd HH:mm"), Width = "17%" },
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
        await LoadDeliveries(page, DeliveryList?.SortBy, DeliveryList?.IsAsc);
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
