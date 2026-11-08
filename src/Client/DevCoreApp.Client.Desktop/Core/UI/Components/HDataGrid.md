# HDataGrid in DevCoreApp

`HDataGrid<TItem>`, `HColumnTemplate`, `GridSettings`, `ColumnDescriptor`, `HSortableHeader`,
`HSearchBar` and `SearchHighlight` come from the **`DevInstance.BlazorToolkit.Components`** NuGet
package (10.7.0+), namespaces `DevInstance.BlazorToolkit.Components.DataGrid` and
`DevInstance.BlazorToolkit.Components.Search` (both imported in `_Imports.razor`). Do not copy
them back into this project; fix bugs upstream in BlazorToolkit.

Component reference (parameters, column descriptors, cell templates, grouping, resizing, search
highlights): <https://github.com/devInstance/BlazorToolkit/blob/master/docs/components.md>.

Host wiring, already in place: Bootstrap 5.3 CSS + JS bundle and Bootstrap Icons in
`wwwroot/index.html`, plus the package stylesheet
`_content/DevInstance.BlazorToolkit.Components/css/blazortoolkit-components.css` (before `app.css`,
so `Styles/` can override it). The grid loads its own JS module; no `<script>` tag.
`PagedList<T>` implements `IModelListHighlights`, so a column with `HighlightField` renders the
server's search matches with no extra code.

This page covers only what is specific to DevCoreApp: persisting the user's layout as a grid
profile, and the conventions pages follow.

## Grid Profile Integration

Pages that use HDataGrid should integrate with `GridProfileService` to persist column order, visibility, grouping, resized widths, sort state, and page size. Follow this pattern; the column part of the profile is mapped by `ApplyProfile` / `ToColumnStates` in
`Core/UI/Model/Grid/GridProfileColumnExtensions.cs` (a new per-column setting is added there, not in every page):

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

    // visibility, grouping, resized width and order (Core/UI/Model/Grid/GridProfileColumnExtensions)
    Columns = Columns.ApplyProfile(profile);
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
        Columns = Columns.ToColumnStates()
    };
    // Not through Host.ServiceSubmitAsync: that flips Host.InProgress, and the grid (IsLoading="Host.InProgress")
    // would flash skeleton rows on every resize / move / sort. A failed preference save needs no banner.
    await GridProfileService.SaveAsync(profileItem);
}

// Event handlers — OnColumnsChanged also fires after a resize or a group change
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

A page-defined `GroupBy` overrides column grouping, so pass `EnableGrouping="false"` (or the
negated toggle, as EmailLog does) to that page's `<GridSettings>` to hide its "Group by" picker.

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

7. **Do not modify or copy HDataGrid** — it is the `DevInstance.BlazorToolkit.Components` package. All customization goes through parameters and templates; grid bugs are fixed in BlazorToolkit.
