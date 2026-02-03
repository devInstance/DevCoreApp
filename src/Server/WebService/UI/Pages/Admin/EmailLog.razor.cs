using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.WebService.Grid;
using DevInstance.DevCoreApp.Server.WebService.Services;
using DevInstance.DevCoreApp.Server.WebService.UI.Components;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.WebServiceToolkit.Common.Model;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Server.WebService.UI.Pages.Admin;

public partial class EmailLog
{
    private const string GridName = "AdminEmailLog";

    [Inject]
    private EmailLogService EmailLogService { get; set; } = default!;

    [Inject]
    private GridProfileService GridProfileService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    private ModelList<EmailLogItem>? EmailLogList { get; set; }

    public List<ColumnDescriptor<EmailLogItem>> Columns { get; set; } = new()
    {
        new() { Label = "From", Field = "fromaddress", ValueSelector = e => e.FromAddress },
        new() { Label = "To", Field = "toaddress", ValueSelector = e => e.ToAddress },
        new() { Label = "Subject", Field = "subject", ValueSelector = e => e.Subject },
        new() { Label = "Status", Field = "status", ValueSelector = e => e.Status },
        new() { Label = "Scheduled Date", Field = "scheduleddate", ValueSelector = e => e.ScheduledDate.ToString("g") },
        new() { Label = "Sent Date", Field = "sentdate", ValueSelector = e => e.SentDate?.ToString("g") ?? string.Empty },
        new() { Label = "Error", Field = "error", ValueSelector = e => e.ErrorMessage ?? string.Empty, IsVisible = false, IsSortable = false },
        new() { Label = "Template", Field = "template", ValueSelector = e => e.TemplateName ?? string.Empty, IsVisible = false, IsSortable = false },
    };

    private int pageCount = 10;
    private string SearchTerm { get; set; } = string.Empty;
    private string SortField { get; set; } = string.Empty;
    private bool IsAsc { get; set; } = true;

    private string StatusFilter { get; set; } = string.Empty;
    private DateTime? StartDateFilter { get; set; }
    private DateTime? EndDateFilter { get; set; }

    private HashSet<string> SelectedIds { get; set; } = new();

    private bool SelectAll
    {
        get => EmailLogList?.Items != null && EmailLogList.Items.Any() && EmailLogList.Items.All(e => SelectedIds.Contains(e.Id));
        set
        {
            if (EmailLogList?.Items == null) return;
            if (value)
            {
                foreach (var item in EmailLogList.Items)
                    SelectedIds.Add(item.Id);
            }
            else
            {
                SelectedIds.Clear();
            }
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadGridProfile();
        await LoadEmailLogs(0, SortField, IsAsc, null);
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

    private int? GetStatusFilterValue()
    {
        if (int.TryParse(StatusFilter, out var val))
            return val;
        return null;
    }

    private async Task LoadEmailLogs(int page, string? sortField, bool? isAsc, string? search)
    {
        await Host.ServiceReadAsync(
            async () => await EmailLogService.GetAllAsync(pageCount, page, sortField, isAsc, search,
                GetStatusFilterValue(), StartDateFilter, EndDateFilter),
            (result) => EmailLogList = result
        );
    }

    public async Task OnPageChangedAsync(int page)
    {
        await LoadEmailLogs(page, EmailLogList?.SortBy, EmailLogList?.IsAsc, EmailLogList?.Search);
    }

    public async Task OnSave(GridSettingsResult<EmailLogItem> grid)
    {
        Columns = grid.Columns;
        var pageSizeChanged = pageCount != grid.PageSize;
        pageCount = grid.PageSize;

        await SaveGridProfile();

        if (pageSizeChanged)
        {
            await LoadEmailLogs(0, SortField, IsAsc, null);
        }
    }

    public async Task OnSearch()
    {
        await LoadEmailLogs(0, EmailLogList?.SortBy, EmailLogList?.IsAsc, SearchTerm);
    }

    public async Task OnClearSearch()
    {
        SearchTerm = string.Empty;
        await LoadEmailLogs(0, EmailLogList?.SortBy, EmailLogList?.IsAsc, null);
    }

    public async Task OnSortAsync(HSortableHeaderSortArgs args)
    {
        SortField = args.SortBy;
        IsAsc = args.IsAscending;
        await SaveGridProfile();
        await LoadEmailLogs(EmailLogList?.Page ?? 0, args.SortBy, args.IsAscending, EmailLogList?.Search);
    }

    public async Task OnApplyFilters()
    {
        await LoadEmailLogs(0, EmailLogList?.SortBy, EmailLogList?.IsAsc, EmailLogList?.Search);
    }

    private void OnRowClick(EmailLogItem item)
    {
        NavigationManager.NavigateTo($"admin/email-log/{item.Id}");
    }

    private void OnRowSelect(string id, bool isSelected)
    {
        if (isSelected)
            SelectedIds.Add(id);
        else
            SelectedIds.Remove(id);
    }

    private async Task OnDeleteSelected()
    {
        if (SelectedIds.Count == 0) return;

        await Host.ServiceSubmitAsync(
            async () => await EmailLogService.DeleteMultipleAsync(SelectedIds.ToList())
        );

        SelectedIds.Clear();
        await LoadEmailLogs(EmailLogList?.Page ?? 0, EmailLogList?.SortBy, EmailLogList?.IsAsc, EmailLogList?.Search);
    }

    private async Task OnResendAllFailed()
    {
        await Host.ServiceSubmitAsync(
            async () => await EmailLogService.ResendAllFailedAsync(
                GetStatusFilterValue(), StartDateFilter, EndDateFilter, EmailLogList?.Search)
        );

        await LoadEmailLogs(EmailLogList?.Page ?? 0, EmailLogList?.SortBy, EmailLogList?.IsAsc, EmailLogList?.Search);
    }
}
