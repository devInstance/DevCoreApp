using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Client.Desktop.Core.UI.Model.Grid;

public class ColumnDescriptor<TItem>
{
    public string Field { get; init; } = default!;
    public string Label { get; init; } = default!;
    public Func<TItem, object?> ValueSelector { get; init; } = default!;

    /// <summary>
    /// The DTO property (<c>nameof(Item.Prop)</c>) whose search highlights apply to this column's value.
    /// When set, a cell without a template renders the value through <c>SearchHighlight</c>, using the
    /// ranges the server put in <c>PagedList.Highlights</c>; a templated cell does it itself.
    /// </summary>
    public string? HighlightField { get; init; }

    public RenderFragment<object?>? Template { get; init; }
    public RenderFragment<TItem>? CellTemplate { get; init; }

    public bool IsVisible { get; set; } = true;

    public bool IsDragable { get; set; } = false;

    public bool IsSortable { get; set; } = true;

    public string Class { get; set; } = string.Empty;

    public string? Width { get; set; }
    public string? HeaderClass { get; set; }
    public string? CellClass { get; set; }
}
