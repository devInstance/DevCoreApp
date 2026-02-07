using DevInstance.DevCoreApp.Server.Admin.WebService.UI.Model.Grid;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Components;

public class GridSettingsResult<TItem>
{
    public List<ColumnDescriptor<TItem>> Columns { get; set; } = new();
    public int PageSize { get; set; }
}

public partial class GridSettings<TItem>
{
    private List<ColumnDescriptor<TItem>> DefaultColumns { get; set; } = default!;

    [Parameter, EditorRequired]
    public List<ColumnDescriptor<TItem>> Columns { get; set; } = default!;

    [Parameter, EditorRequired]
    public EventCallback<GridSettingsResult<TItem>> OnSave { get; set; }

    [Parameter]
    public int PageSize { get; set; } = 10;

    private bool EnableResetButton = false;
    private ColumnDescriptor<TItem>? draggableEntry = null;

    protected override Task OnParametersSetAsync()
    {
        DefaultColumns = Columns.ToList();
        return Task.CompletedTask;
    }

    private async Task DoSave()
    {
        var result = new GridSettingsResult<TItem>
        {
            Columns = Columns,
            PageSize = PageSize
        };
        await OnSave.InvokeAsync(result);
    }

    private void DoReset()
    {
        Columns = DefaultColumns.ToList();
        EnableResetButton = false;
        StateHasChanged();
    }

    public void HandleMouseDown(ColumnDescriptor<TItem> entry)
    {
        entry.IsDragable = true;
    }

    public void HandleMouseUp(ColumnDescriptor<TItem> entry)
    {
        entry.IsDragable = false;
    }

    public void HandleDragEnter(ColumnDescriptor<TItem> entry)
    {
        if (draggableEntry?.Field == entry.Field) return;
        entry.Class = "column-can-drop";
    }

    public void HandleDragLeave(ColumnDescriptor<TItem> entry)
    {
        entry.Class = "";
    }

    public void HandleDrop(ColumnDescriptor<TItem> entry)
    {
        entry.Class = "";
        entry.IsDragable = false;

        if (draggableEntry != null && draggableEntry.Field == entry.Field) return;

        int newIndex = -1;
        int oldIndex = -1;
        for (int i = 0; i < Columns.Count; i++)
        {
            var col = Columns[i];
            if (col.Field == entry.Field)
            {
                newIndex = i;
            }
            else if (draggableEntry != null && col.Field == draggableEntry.Field)
            {
                oldIndex = i;
            }
        }

        if (newIndex >= 0 && oldIndex >= 0)
        {
            var ent = Columns[oldIndex];
            Columns.RemoveAt(oldIndex);
            Columns.Insert(newIndex, ent);
        }

        EnableResetButton = true;
        draggableEntry = null;
    }

    public void HandleDragStart(ColumnDescriptor<TItem> entry)
    {
        if (draggableEntry != null && draggableEntry.Field == entry.Field) return;

        draggableEntry = entry;
        entry.Class = "column-dragging";
    }
}
