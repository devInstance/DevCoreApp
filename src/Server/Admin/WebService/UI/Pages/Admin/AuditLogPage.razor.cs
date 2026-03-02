using System.Text.Json;
using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Admin.Services;
using DevInstance.DevCoreApp.Server.Admin.Services.AuditLogs;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Components;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Model.Grid;
using DevInstance.DevCoreApp.Shared.Model.AuditLogs;
using DevInstance.DevCoreApp.Shared.Model.Settings;
using DevInstance.WebServiceToolkit.Common.Model;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.Admin;

public partial class AuditLogPage
{
    private const string GridName = "AdminAuditLog";

    [Inject]
    private IAuditLogService AuditLogService { get; set; } = default!;

    [Inject]
    private GridProfileService GridProfileService { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    private ModelList<AuditLogItem>? AuditLogList { get; set; }

    public List<ColumnDescriptor<AuditLogItem>> Columns { get; set; } = new()
    {
        new() { Label = "Timestamp", Field = "changedat", ValueSelector = a => a.ChangedAt.ToString("g"), Width = "14%" },
        new() { Label = "User", Field = "user", ValueSelector = a => a.ChangedByUserName ?? "N/A", IsSortable = false, Width = "14%" },
        new() { Label = "Table", Field = "tablename", ValueSelector = a => a.TableName, Width = "14%" },
        new() { Label = "Record ID", Field = "recordid", ValueSelector = a => a.RecordId, Width = "14%" },
        new() { Label = "Action", Field = "action", ValueSelector = a => a.Action, Width = "10%" },
        new() { Label = "Source", Field = "source", ValueSelector = a => a.Source, Width = "12%" },
        new() { Label = "IP Address", Field = "ipaddress", ValueSelector = a => a.IpAddress ?? string.Empty, IsVisible = false, IsSortable = false, Width = "12%" },
        new() { Label = "Correlation ID", Field = "correlationid", ValueSelector = a => a.CorrelationId ?? string.Empty, IsVisible = false, IsSortable = false, Width = "14%" },
    };

    private int pageCount = 15;
    private string SearchTerm { get; set; } = string.Empty;
    private string SortField { get; set; } = string.Empty;
    private bool IsAsc { get; set; } = true;

    private string ActionFilter { get; set; } = string.Empty;
    private string SourceFilter { get; set; } = string.Empty;
    private string TableNameFilter { get; set; } = string.Empty;
    private string RecordIdFilter { get; set; } = string.Empty;
    private DateTime? StartDateFilter { get; set; }
    private DateTime? EndDateFilter { get; set; }

    private AuditLogItem? SelectedEntry { get; set; }
    private Dictionary<string, string>? OldValuesMap { get; set; }
    private Dictionary<string, string>? NewValuesMap { get; set; }
    private List<string>? AllPropertyKeys { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadGridProfile();
        await LoadAuditLogs(0, SortField, IsAsc, null);
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

    private int? GetActionFilterValue()
    {
        if (int.TryParse(ActionFilter, out var val))
            return val;
        return null;
    }

    private int? GetSourceFilterValue()
    {
        if (int.TryParse(SourceFilter, out var val))
            return val;
        return null;
    }

    private async Task LoadAuditLogs(int page, string? sortField, bool? isAsc, string? search)
    {
        await Host.ServiceReadAsync(
            async () => await AuditLogService.GetAllAsync(pageCount, page, sortField, isAsc, search,
                GetActionFilterValue(), GetSourceFilterValue(),
                string.IsNullOrEmpty(TableNameFilter) ? null : TableNameFilter,
                string.IsNullOrEmpty(RecordIdFilter) ? null : RecordIdFilter,
                StartDateFilter, EndDateFilter),
            (result) => AuditLogList = result
        );
    }

    public async Task OnPageChangedAsync(int page)
    {
        await LoadAuditLogs(page, AuditLogList?.SortBy, AuditLogList?.IsAsc, AuditLogList?.Search);
    }

    public async Task OnSave(GridSettingsResult<AuditLogItem> grid)
    {
        Columns = grid.Columns;
        var pageSizeChanged = pageCount != grid.PageSize;
        pageCount = grid.PageSize;

        await SaveGridProfile();

        if (pageSizeChanged)
        {
            await LoadAuditLogs(0, SortField, IsAsc, null);
        }
    }

    public async Task OnSearch()
    {
        await LoadAuditLogs(0, AuditLogList?.SortBy, AuditLogList?.IsAsc, SearchTerm);
    }

    public async Task OnClearSearch()
    {
        SearchTerm = string.Empty;
        await LoadAuditLogs(0, AuditLogList?.SortBy, AuditLogList?.IsAsc, null);
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
        await LoadAuditLogs(AuditLogList?.Page ?? 0, args.SortBy, args.IsAscending, AuditLogList?.Search);
    }

    public async Task OnApplyFilters()
    {
        SelectedEntry = null;
        OldValuesMap = null;
        NewValuesMap = null;
        AllPropertyKeys = null;
        await LoadAuditLogs(0, AuditLogList?.SortBy, AuditLogList?.IsAsc, AuditLogList?.Search);
    }

    private Task OnRowClick(AuditLogItem item)
    {
        if (SelectedEntry?.Id == item.Id)
        {
            ClearSelection();
        }
        else
        {
            SelectedEntry = item;
            OldValuesMap = ParseJson(item.OldValues);
            NewValuesMap = ParseJson(item.NewValues);
            AllPropertyKeys = ComputeAllKeys(OldValuesMap, NewValuesMap);
        }
        return Task.CompletedTask;
    }

    private void ClearSelection()
    {
        SelectedEntry = null;
        OldValuesMap = null;
        NewValuesMap = null;
        AllPropertyKeys = null;
    }

    private static Dictionary<string, string>? ParseJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var result = new Dictionary<string, string>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                result[prop.Name] = prop.Value.ValueKind == JsonValueKind.Null
                    ? ""
                    : prop.Value.ToString();
            }
            return result;
        }
        catch
        {
            return null;
        }
    }

    private static List<string> ComputeAllKeys(Dictionary<string, string>? oldMap, Dictionary<string, string>? newMap)
    {
        var keys = new HashSet<string>();
        if (oldMap != null)
        {
            foreach (var k in oldMap.Keys)
                keys.Add(k);
        }
        if (newMap != null)
        {
            foreach (var k in newMap.Keys)
                keys.Add(k);
        }
        return keys.OrderBy(k => k).ToList();
    }

    private static string GetActionBadgeClass(string action)
    {
        return action switch
        {
            "Insert" => "bg-success",
            "Update" => "bg-primary",
            "Delete" => "bg-danger",
            _ => "bg-secondary"
        };
    }
}
