using DevInstance.DevCoreApp.Shared.Model.Core.Settings;
using DevInstance.BlazorToolkit.Components.DataGrid;

namespace DevInstance.DevCoreApp.Client.Desktop.Core.UI.Model.Grid;

/// <summary>
/// Maps grid columns to and from the column part of a saved <see cref="GridProfileItem"/>. Pages keep the rest of
/// the profile (page size, sort, grid name) themselves; a new per-column setting only needs adding here.
/// </summary>
public static class GridProfileColumnExtensions
{
    /// <summary>
    /// Applies the saved visibility, width and grouping to <paramref name="columns"/> and returns them in the saved
    /// order. Columns the profile does not know keep their defaults and go last; saved fields with no column are ignored.
    /// </summary>
    public static List<ColumnDescriptor<TItem>> ApplyProfile<TItem>(this List<ColumnDescriptor<TItem>> columns, GridProfileItem profile)
    {
        foreach (var state in profile.Columns)
        {
            var column = columns.FirstOrDefault(c => c.Field == state.Field);
            if (column == null) continue;
            column.IsVisible = state.IsVisible;
            column.ResizedWidth = state.Width;
            column.IsGrouped = state.IsGrouped;
        }

        if (profile.Columns.Count == 0)
            return columns;

        return columns
            .OrderBy(c => profile.Columns.FindIndex(cs => cs.Field == c.Field) is var idx && idx >= 0 ? idx : int.MaxValue)
            .ToList();
    }

    /// <summary>The columns' current state, in display order, for <see cref="GridProfileItem.Columns"/>.</summary>
    public static List<GridColumnState> ToColumnStates<TItem>(this IEnumerable<ColumnDescriptor<TItem>> columns) =>
        columns.Select((c, i) => new GridColumnState
        {
            Field = c.Field,
            IsVisible = c.IsVisible,
            Order = i,
            Width = c.ResizedWidth,
            IsGrouped = c.IsGrouped
        }).ToList();
}
