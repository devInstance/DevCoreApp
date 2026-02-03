using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Server.WebService.UI.Model.Grid;

public class ColumnDescriptor<TItem>
{
    public string Field { get; init; } = default!;
    public string Label { get; init; } = default!;
    public Func<TItem, object?> ValueSelector { get; init; } = default!;

    public RenderFragment<object?>? Template { get; init; }

    public bool IsVisible { get; set; } = true;

    public bool IsDragable { get; set; } = false;

    public bool IsSortable { get; set; } = true;

    public string Class { get; set; } = string.Empty;
}
