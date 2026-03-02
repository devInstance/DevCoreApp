using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Admin.Services;
using DevInstance.DevCoreApp.Server.Admin.Services.BackgroundTasks;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Components;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Model.Grid;
using DevInstance.DevCoreApp.Shared.Model.BackgroundTasks;
using DevInstance.DevCoreApp.Shared.Model.Settings;
using DevInstance.WebServiceToolkit.Common.Model;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.Admin;

public partial class JobDashboardPage
{
    private const string GridName = "AdminJobDashboard";

    [Inject]
    private IJobDashboardService JobDashboardService { get; set; } = default!;

    [Inject]
    private GridProfileService GridProfileService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "search")]
    private string? InitialSearch { get; set; }

    private ModelList<BackgroundTaskItem>? JobList { get; set; }

    public List<ColumnDescriptor<BackgroundTaskItem>> Columns { get; set; } = new()
    {
        new() { Label = "Task Type", Field = "tasktype", ValueSelector = t => t.TaskType, Width = "14%" },
        new() { Label = "Status", Field = "status", ValueSelector = t => t.Status, Width = "10%" },
        new() { Label = "Scheduled", Field = "scheduledat", ValueSelector = t => t.ScheduledAt.ToString("g"), Width = "14%" },
        new() { Label = "Started", Field = "startedat", ValueSelector = t => t.StartedAt?.ToString("g") ?? string.Empty, Width = "14%" },
        new() { Label = "Completed", Field = "completedat", ValueSelector = t => t.CompletedAt?.ToString("g") ?? string.Empty, Width = "14%" },
        new() { Label = "Retries", Field = "retrycount", ValueSelector = t => $"{t.RetryCount}/{t.MaxRetries}", IsSortable = false, Width = "8%" },
        new() { Label = "Result Ref", Field = "resultreference", ValueSelector = t => t.ResultReference ?? string.Empty, Width = "14%" },
        new() { Label = "Created By", Field = "createdbyname", ValueSelector = t => t.CreatedByName ?? string.Empty, Width = "12%" },
        new() { Label = "Error", Field = "errormessage", ValueSelector = t => t.ErrorMessage ?? string.Empty, IsVisible = false, IsSortable = false, Width = "20%" },
    };

    private int pageCount = 10;
    private string SearchTerm { get; set; } = string.Empty;
    private string SortField { get; set; } = string.Empty;
    private bool IsAsc { get; set; } = true;

    private string StatusFilter { get; set; } = string.Empty;
    private string TaskTypeFilter { get; set; } = string.Empty;
    private DateTime? StartDateFilter { get; set; }
    private DateTime? EndDateFilter { get; set; }

    private BackgroundTaskItem? SelectedJob { get; set; }
    private List<BackgroundTaskLogItem>? SelectedJobLogs { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrEmpty(InitialSearch))
        {
            SearchTerm = InitialSearch;
        }

        await LoadGridProfile();
        await LoadJobs(0, SortField, IsAsc, string.IsNullOrEmpty(SearchTerm) ? null : SearchTerm);
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

    private async Task LoadJobs(int page, string? sortField, bool? isAsc, string? search)
    {
        await Host.ServiceReadAsync(
            async () => await JobDashboardService.GetAllAsync(pageCount, page, sortField, isAsc, search,
                GetStatusFilterValue(), string.IsNullOrEmpty(TaskTypeFilter) ? null : TaskTypeFilter,
                StartDateFilter, EndDateFilter),
            (result) => JobList = result
        );
    }

    public async Task OnPageChangedAsync(int page)
    {
        await LoadJobs(page, JobList?.SortBy, JobList?.IsAsc, JobList?.Search);
    }

    public async Task OnSave(GridSettingsResult<BackgroundTaskItem> grid)
    {
        Columns = grid.Columns;
        var pageSizeChanged = pageCount != grid.PageSize;
        pageCount = grid.PageSize;

        await SaveGridProfile();

        if (pageSizeChanged)
        {
            await LoadJobs(0, SortField, IsAsc, null);
        }
    }

    public async Task OnSearch()
    {
        await LoadJobs(0, JobList?.SortBy, JobList?.IsAsc, SearchTerm);
    }

    public async Task OnClearSearch()
    {
        SearchTerm = string.Empty;
        await LoadJobs(0, JobList?.SortBy, JobList?.IsAsc, null);
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
        await LoadJobs(JobList?.Page ?? 0, args.SortBy, args.IsAscending, JobList?.Search);
    }

    public async Task OnApplyFilters()
    {
        SelectedJob = null;
        SelectedJobLogs = null;
        await LoadJobs(0, JobList?.SortBy, JobList?.IsAsc, JobList?.Search);
    }

    private async Task OnRowClick(BackgroundTaskItem item)
    {
        if (SelectedJob?.Id == item.Id)
        {
            SelectedJob = null;
            SelectedJobLogs = null;
            return;
        }

        SelectedJob = item;
        SelectedJobLogs = null;

        await Host.ServiceReadAsync(
            async () => await JobDashboardService.GetJobLogsAsync(item.Id),
            (result) => SelectedJobLogs = result
        );
    }

    private void ClearSelection()
    {
        SelectedJob = null;
        SelectedJobLogs = null;
    }

    private async Task CancelJob()
    {
        if (SelectedJob == null) return;

        await Host.ServiceSubmitAsync(
            async () => await JobDashboardService.CancelJobAsync(SelectedJob.Id)
        );

        SelectedJob = null;
        SelectedJobLogs = null;
        await LoadJobs(JobList?.Page ?? 0, JobList?.SortBy, JobList?.IsAsc, JobList?.Search);
    }

    private async Task RetryJob()
    {
        if (SelectedJob == null) return;

        await Host.ServiceSubmitAsync(
            async () => await JobDashboardService.RetryJobAsync(SelectedJob.Id)
        );

        SelectedJob = null;
        SelectedJobLogs = null;
        await LoadJobs(JobList?.Page ?? 0, JobList?.SortBy, JobList?.IsAsc, JobList?.Search);
    }

    private static string GetStatusBadgeClass(string status)
    {
        return status switch
        {
            "Queued" => "bg-secondary",
            "Running" => "bg-primary",
            "Completed" => "bg-success",
            "Failed" => "bg-danger",
            _ => "bg-light text-dark"
        };
    }

    private static string GetLogStatusBadgeClass(string status)
    {
        return status switch
        {
            "Running" => "bg-primary",
            "Completed" => "bg-success",
            "Failed" => "bg-danger",
            _ => "bg-light text-dark"
        };
    }
}
