# HDataGrid Component

`HDataGrid<TItem>` is a generic data grid component for displaying `ModelList<TItem>` data with sorting, paging, row selection, grouping, column visibility management, and a column header context menu.

**Location:** `UI/Components/HDataGrid.razor` / `.razor.cs`

## Quick Start

### 1. Define columns in code-behind

```csharp
public List<ColumnDescriptor<MyItem>> Columns { get; set; } = new()
{
    new() { Label = "Name",   Field = "name",   ValueSelector = x => x.Name },
    new() { Label = "Email",  Field = "email",  ValueSelector = x => x.Email },
    new() { Label = "Status", Field = "status", ValueSelector = x => x.Status.ToString() },
    new() { Label = "Notes",  Field = "notes",  ValueSelector = x => x.Notes, IsSortable = false, IsVisible = false },
};
```

### 2. Place the grid in Razor

```razor
<HDataGrid TItem="MyItem"
           Data="ItemList"
           Columns="Columns"
           IsLoading="Host.InProgress"
           OnSort="OnSortAsync"
           OnPageChanged="OnPageChangedAsync"
           OnColumnsChanged="OnColumnsChanged">
    <EmptyContent>
        <p>No items found.</p>
    </EmptyContent>
</HDataGrid>
```

### 3. Add GridSettings offcanvas

Place `<GridSettings>` alongside the grid to enable the settings panel (opened via the context menu "Settings" item or the floating button):

```razor
<GridSettings Columns="Columns" OnSave="OnSave" PageSize="pageCount" TItem="MyItem" />
```

## Parameters

### Required

| Parameter | Type | Description |
|-----------|------|-------------|
| `TItem` | type param | The item type (must inherit `ModelItem`) |
| `Data` | `ModelList<TItem>?` | The data source |
| `Columns` | `List<ColumnDescriptor<TItem>>` | Column definitions |

### Data & Loading

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `IsLoading` | `bool` | `false` | Shows skeleton placeholder rows |
| `PlaceholderRows` | `int` | `5` | Number of skeleton rows while loading |

### Sorting & Paging

| Parameter | Type | Description |
|-----------|------|-------------|
| `OnSort` | `EventCallback<HSortableHeaderSortArgs>` | Fires on column sort (click or context menu) |
| `OnPageChanged` | `EventCallback<int>` | Fires on page navigation |
| `ShowAllEnabled` | `bool` | Show "Show All / Show Paged" toggle |
| `ShowAll` | `bool` | Current show-all state |
| `OnShowAllChanged` | `EventCallback<bool>` | Fires when show-all is toggled |

### Selection

| Parameter | Type | Description |
|-----------|------|-------------|
| `EnableSelection` | `bool` | Show checkbox column |
| `SelectedIds` | `HashSet<string>?` | Currently selected IDs |
| `SelectedIdsChanged` | `EventCallback<HashSet<string>>` | Fires on selection change |
| `IdSelector` | `Func<TItem, string>?` | Extracts ID from item |

### Row Behavior

| Parameter | Type | Description |
|-----------|------|-------------|
| `OnRowClick` | `EventCallback<TItem>` | Fires on row click (adds pointer cursor) |
| `RowClass` | `Func<TItem, string>?` | CSS class per row |
| `TableClass` | `string?` | Additional CSS class on `<table>` |

### Grouping

| Parameter | Type | Description |
|-----------|------|-------------|
| `GroupBy` | `Func<TItem, object?>?` | Group key selector (null = no grouping) |
| `GroupHeaderTemplate` | `RenderFragment<GroupContext<TItem>>?` | Custom group header |
| `GroupsCollapsedByDefault` | `bool` | Start groups collapsed |

### Column Management

| Parameter | Type | Description |
|-----------|------|-------------|
| `OnColumnsChanged` | `EventCallback` | Fires after move/hide from context menu |
| `ColumnTemplates` | `RenderFragment?` | Container for `<HColumnTemplate>` declarations |
| `BeforeHeaderRow` | `RenderFragment?` | Extra rows before the header `<tr>` |

## ColumnDescriptor Properties

```csharp
public class ColumnDescriptor<TItem>
{
    string   Field          // Unique field key (used for sort, profile, template matching)
    string   Label          // Display header text
    Func<TItem, object?> ValueSelector  // Extracts display value from item

    // Templates (see "Cell Templates" section)
    RenderFragment<object?>? Template      // Value-level template (receives ValueSelector output)
    RenderFragment<TItem>?   CellTemplate  // Item-level template (receives full item)

    bool     IsVisible      // true — show/hide, persisted in grid profile
    bool     IsSortable     // true — enables sort header and context menu sort items
    bool     IsDragable     // false — internal, used by GridSettings drag-and-drop
    string   Class          // internal, used by GridSettings drag state

    string?  Width          // Column width (e.g., "200px", "30%")
    string?  HeaderClass    // CSS class on <th>
    string?  CellClass      // CSS class on <td>
}
```

## Cell Templates

There are three ways to render cell content, checked in this priority order:

### 1. Razor `<HColumnTemplate>` (preferred for rich markup)

Define templates declaratively in Razor using the `ColumnTemplates` slot. The `Field` must match a column's `Field` value.

```razor
<HDataGrid TItem="MyItem" ...>
    <ColumnTemplates>
        <HColumnTemplate TItem="MyItem" Field="actions" Context="item">
            <a href="items/@item.Id/edit" class="btn btn-sm btn-outline-primary">
                <i class="bi bi-pencil"></i>
            </a>
            <button class="btn btn-sm btn-outline-danger" @onclick="() => Delete(item)">
                <i class="bi bi-trash"></i>
            </button>
        </HColumnTemplate>
    </ColumnTemplates>
    ...
</HDataGrid>
```

### 2. `CellTemplate` on ColumnDescriptor (for code-behind templates)

Set `CellTemplate` in the column definition. Receives the full `TItem`. Use only when Razor templates are not feasible.

```csharp
new() {
    Label = "Actions", Field = "actions", ValueSelector = u => u.Id, IsSortable = false,
    CellTemplate = item => builder => { /* RenderTreeBuilder code */ }
}
```

### 3. `Template` on ColumnDescriptor (for value formatting)

Set `Template` for formatting the value returned by `ValueSelector`:

```csharp
new() {
    Label = "Status", Field = "status",
    ValueSelector = e => e.Status,
    Template = value => @<span class="badge bg-info">@value</span>
}
```

### 4. Default

If none of the above are set, the grid renders `@col.ValueSelector(item)` as plain text.

## Column Header Context Menu

Right-clicking any column header in the data table opens a context menu with:

| Item | Icon | Action | Disabled when |
|------|------|--------|---------------|
| Sort Asc | `bi-sort-down-alt` | Sort ascending | Column not sortable |
| Sort Desc | `bi-sort-up-alt` | Sort descending | Column not sortable |
| Move Left | `bi-chevron-left` | Swap with left neighbor | First column |
| Move Right | `bi-chevron-right` | Swap with right neighbor | Last column |
| Hide | `bi-eye-slash` | Set `IsVisible = false` | — |
| Settings | `bi-gear` | Open GridSettings offcanvas | — |

Move and Hide actions fire `OnColumnsChanged`. The page handler should persist the grid profile.

## Grid Profile Integration

Pages that use HDataGrid should integrate with `GridProfileService` to persist column order, visibility, sort state, and page size. Follow this pattern:

### Code-behind boilerplate

```csharp
private const string GridName = "AdminMyEntity";    // unique grid name
private int pageCount = 10;
private string SortField { get; set; } = string.Empty;
private bool IsAsc { get; set; } = true;

[Inject] private GridProfileService GridProfileService { get; set; } = default!;

protected override async Task OnInitializedAsync()
{
    await LoadGridProfile();
    await LoadData(0, SortField, IsAsc, null);
}

// Load saved profile (column order, visibility, sort, page size)
private async Task LoadGridProfile()
{
    await Host.ServiceReadAsync(
        async () => await GridProfileService.GetAsync(GridName),
        (profile) => { if (profile != null) ApplyGridProfile(profile); }
    );
}

// Apply profile to local state
private void ApplyGridProfile(GridProfileItem profile)
{
    pageCount = profile.PageSize;
    SortField = profile.SortField ?? string.Empty;
    IsAsc = profile.IsAsc;

    foreach (var cs in profile.Columns)
    {
        var col = Columns.FirstOrDefault(c => c.Field == cs.Field);
        if (col != null) col.IsVisible = cs.IsVisible;
    }

    if (profile.Columns.Count > 0)
    {
        Columns = Columns
            .OrderBy(c => profile.Columns.FindIndex(cs => cs.Field == c.Field) is var idx && idx >= 0 ? idx : int.MaxValue)
            .ToList();
    }
}

// Persist current state
private async Task SaveGridProfile()
{
    var profileItem = new GridProfileItem
    {
        GridName = GridName,
        ProfileName = "Default",
        PageSize = pageCount,
        SortField = string.IsNullOrEmpty(SortField) ? null : SortField,
        IsAsc = IsAsc,
        Columns = Columns.Select((c, i) => new GridColumnState
        {
            Field = c.Field, IsVisible = c.IsVisible, Order = i
        }).ToList()
    };
    await Host.ServiceSubmitAsync(async () => await GridProfileService.SaveAsync(profileItem));
}

// Event handlers
public async Task OnColumnsChanged() => await SaveGridProfile();

public async Task OnSortAsync(HSortableHeaderSortArgs args)
{
    SortField = args.SortBy;
    IsAsc = args.IsAscending;
    await SaveGridProfile();
    await LoadData(CurrentPage, args.SortBy, args.IsAscending, CurrentSearch);
}

public async Task OnSave(GridSettingsResult<MyItem> grid)
{
    Columns = grid.Columns;
    var pageSizeChanged = pageCount != grid.PageSize;
    pageCount = grid.PageSize;
    await SaveGridProfile();
    if (pageSizeChanged) await LoadData(0, SortField, IsAsc, null);
}
```

## Grouping Example

```razor
<HDataGrid TItem="EmailLogItem"
           ...
           GroupBy="@(GroupByStatus ? (e => e.Status) : null)"
           GroupsCollapsedByDefault="false">
    <GroupHeaderTemplate>
        <strong>@context.Key</strong>
        <span class="badge bg-secondary ms-2">@context.Count</span>
    </GroupHeaderTemplate>
</HDataGrid>
```

`GroupContext<TItem>` provides `Key` (the group key), `Items` (list of items), and `Count`.

## Selection Example

```razor
<HDataGrid TItem="EmailLogItem"
           ...
           EnableSelection="true"
           SelectedIds="SelectedIds"
           SelectedIdsChanged="OnSelectionChanged"
           IdSelector="e => e.Id">
</HDataGrid>
```

The grid renders a select-all checkbox in the header and per-row checkboxes. Manage `SelectedIds` in the page.

## Reference Pages

- **Users** (`Pages/Admin/Users.razor`): Basic grid with sort, paging, column templates, grid profile.
- **EmailLog** (`Pages/Admin/EmailLog.razor`): Full-featured grid with selection, grouping, row click, search, grid profile.

---

## Agent Instructions

When creating a new page that displays a list of items using HDataGrid:

1. **Always use HDataGrid** — do not write inline `<table>` markup for list pages. Use HDataGrid for all tabular data displays.

2. **Define columns in the code-behind** as `List<ColumnDescriptor<TItem>>`. Set `Field` to a unique lowercase key matching the API sort field. Set `IsSortable = false` for computed/non-sortable columns.

3. **Use `<HColumnTemplate>` in Razor** for custom cell rendering (action buttons, badges, links). Do not use `CellTemplate` with `RenderTreeBuilder` in code-behind — keep templates in Razor markup.

4. **Always include GridSettings** — place `<GridSettings>` after `<HDataGrid>` to enable the settings offcanvas panel.

5. **Always wire up grid profile persistence** — follow the `LoadGridProfile` / `ApplyGridProfile` / `SaveGridProfile` pattern from the Users page. Connect `OnColumnsChanged`, `OnSort`, and `OnSave` to save the profile.

6. **Use the full type path** for `TItem` in Razor: `TItem="DevInstance.DevCoreApp.Shared.Model.MyItem"` (also on `HColumnTemplate` and `GridSettings`).

7. **Do not modify HDataGrid internals** to handle page-specific logic. All customization goes through parameters and templates.
