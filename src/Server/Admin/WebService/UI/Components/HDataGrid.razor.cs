using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Model.Grid;
using DevInstance.WebServiceToolkit.Common.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Components;

public partial class HDataGrid<TItem> where TItem : ModelItem
{
    private static readonly int[] PlaceholderWidths = [75, 60, 85, 70, 90, 65, 80];

    private readonly Dictionary<object?, bool> _expandedGroups = new();
    private readonly Dictionary<string, RenderFragment<TItem>> _columnTemplates = new();

    private ColumnDescriptor<TItem>? _contextMenuColumn;
    private double _contextMenuX;
    private double _contextMenuY;

    [Parameter, EditorRequired]
    public ModelList<TItem>? Data { get; set; }

    [Parameter, EditorRequired]
    public List<ColumnDescriptor<TItem>> Columns { get; set; } = [];

    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public int PlaceholderRows { get; set; } = 5;

    [Parameter]
    public EventCallback<HSortableHeaderSortArgs> OnSort { get; set; }

    [Parameter]
    public EventCallback<int> OnPageChanged { get; set; }

    [Parameter]
    public bool ShowAllEnabled { get; set; }

    [Parameter]
    public bool ShowAll { get; set; }

    [Parameter]
    public EventCallback<bool> OnShowAllChanged { get; set; }

    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    [Parameter]
    public RenderFragment<string>? SearchContent { get; set; }

    [Parameter]
    public bool EnableSelection { get; set; }

    [Parameter]
    public HashSet<string>? SelectedIds { get; set; }

    [Parameter]
    public EventCallback<HashSet<string>> SelectedIdsChanged { get; set; }

    [Parameter]
    public Func<TItem, string>? IdSelector { get; set; }

    [Parameter]
    public EventCallback<TItem> OnRowClick { get; set; }

    [Parameter]
    public Func<TItem, object?>? GroupBy { get; set; }

    [Parameter]
    public RenderFragment<GroupContext<TItem>>? GroupHeaderTemplate { get; set; }

    [Parameter]
    public bool GroupsCollapsedByDefault { get; set; }

    [Parameter]
    public string? TableClass { get; set; }

    [Parameter]
    public Func<TItem, string>? RowClass { get; set; }

    [Parameter]
    public RenderFragment? BeforeHeaderRow { get; set; }

    [Parameter]
    public RenderFragment? ColumnTemplates { get; set; }

    [Parameter]
    public EventCallback OnColumnsChanged { get; set; }

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    internal void RegisterColumnTemplate(string field, RenderFragment<TItem> template)
    {
        _columnTemplates[field] = template;
    }

    private RenderFragment<TItem>? GetCellTemplate(ColumnDescriptor<TItem> col)
    {
        if (_columnTemplates.TryGetValue(col.Field, out var template))
            return template;
        return col.CellTemplate;
    }

    private IEnumerable<ColumnDescriptor<TItem>> VisibleColumns => Columns.Where(c => c.IsVisible);

    private int TotalColumnCount => VisibleColumns.Count() + (EnableSelection ? 1 : 0);

    private bool IsAllSelected =>
        Data?.Items != null
        && Data.Items.Any()
        && SelectedIds != null
        && IdSelector != null
        && Data.Items.All(item => SelectedIds.Contains(IdSelector(item)));

    private void OnSelectAllChanged(ChangeEventArgs e)
    {
        if (Data?.Items == null || SelectedIds == null || IdSelector == null) return;

        var selectAll = (bool)(e.Value ?? false);
        if (selectAll)
        {
            foreach (var item in Data.Items)
                SelectedIds.Add(IdSelector(item));
        }
        else
        {
            foreach (var item in Data.Items)
                SelectedIds.Remove(IdSelector(item));
        }

        SelectedIdsChanged.InvokeAsync(SelectedIds);
    }

    private void OnRowSelectChanged(TItem item, bool isSelected)
    {
        if (SelectedIds == null || IdSelector == null) return;

        var id = IdSelector(item);
        if (isSelected)
            SelectedIds.Add(id);
        else
            SelectedIds.Remove(id);

        SelectedIdsChanged.InvokeAsync(SelectedIds);
    }

    private List<GroupContext<TItem>> GetGroups()
    {
        if (Data?.Items == null || GroupBy == null) return [];

        return Data.Items
            .GroupBy(GroupBy)
            .Select(g => new GroupContext<TItem>
            {
                Key = g.Key,
                Items = g.ToList(),
                Count = g.Count()
            })
            .ToList();
    }

    private bool IsGroupExpanded(object? key)
    {
        if (_expandedGroups.TryGetValue(key ?? "__null__", out var expanded))
            return expanded;

        return !GroupsCollapsedByDefault;
    }

    private void ToggleGroup(object? key)
    {
        var normalizedKey = key ?? "__null__";
        _expandedGroups[normalizedKey] = !IsGroupExpanded(key);
    }

    private async Task ToggleShowAll()
    {
        await OnShowAllChanged.InvokeAsync(!ShowAll);
    }

    // Context menu

    private bool IsContextMenuVisible => _contextMenuColumn != null;

    private bool IsContextMenuColumnSortable => _contextMenuColumn?.IsSortable == true;

    private bool IsFirstVisibleColumn =>
        _contextMenuColumn != null && Columns.IndexOf(_contextMenuColumn) == 0;

    private bool IsLastVisibleColumn =>
        _contextMenuColumn != null && Columns.IndexOf(_contextMenuColumn) == Columns.Count - 1;

    private void OnHeaderContextMenu(MouseEventArgs e, ColumnDescriptor<TItem> col)
    {
        _contextMenuColumn = col;
        _contextMenuX = e.ClientX;
        _contextMenuY = e.ClientY;
    }

    private void CloseContextMenu()
    {
        _contextMenuColumn = null;
    }

    private async Task ContextMenuSortAsc()
    {
        if (_contextMenuColumn is { IsSortable: true })
        {
            await OnSort.InvokeAsync(new HSortableHeaderSortArgs(_contextMenuColumn.Field, true));
        }
        CloseContextMenu();
    }

    private async Task ContextMenuSortDesc()
    {
        if (_contextMenuColumn is { IsSortable: true })
        {
            await OnSort.InvokeAsync(new HSortableHeaderSortArgs(_contextMenuColumn.Field, false));
        }
        CloseContextMenu();
    }

    private async Task ContextMenuMoveLeft()
    {
        if (_contextMenuColumn != null)
        {
            var idx = Columns.IndexOf(_contextMenuColumn);
            if (idx > 0)
            {
                Columns.RemoveAt(idx);
                Columns.Insert(idx - 1, _contextMenuColumn);
                await OnColumnsChanged.InvokeAsync();
            }
        }
        CloseContextMenu();
    }

    private async Task ContextMenuMoveRight()
    {
        if (_contextMenuColumn != null)
        {
            var idx = Columns.IndexOf(_contextMenuColumn);
            if (idx >= 0 && idx < Columns.Count - 1)
            {
                Columns.RemoveAt(idx);
                Columns.Insert(idx + 1, _contextMenuColumn);
                await OnColumnsChanged.InvokeAsync();
            }
        }
        CloseContextMenu();
    }

    private async Task ContextMenuHideColumn()
    {
        if (_contextMenuColumn != null)
        {
            _contextMenuColumn.IsVisible = false;
            await OnColumnsChanged.InvokeAsync();
        }
        CloseContextMenu();
    }

    private async Task ContextMenuShowSettings()
    {
        CloseContextMenu();
        await JS.InvokeVoidAsync("eval",
            "bootstrap.Offcanvas.getOrCreateInstance(document.querySelector('#offcanvasGridSettings')).show()");
    }
}
