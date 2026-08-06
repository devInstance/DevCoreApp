namespace DevInstance.DevCoreApp.Server.Admin.WebService.Core.UI.Model.Grid;

public class GroupContext<TItem>
{
    public object? Key { get; init; }
    public IReadOnlyList<TItem> Items { get; init; } = [];
    public int Count { get; init; }
}
