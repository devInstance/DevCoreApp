namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

/// <summary>
/// Interface for query that can be sorted by column name.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IQSortable<T>
{
    /// <summary>
    /// Sort by column name.
    /// </summary>
    /// <param name="column"></param>
    /// <param name="isAsc"></param>
    /// <returns>An instance of the implementing class with the updated state.</returns>
    T SortBy(string column, bool isAsc);
    /// <summary>
    /// Column name result is sorted by
    /// </summary>
    string SortedBy { get; }
    /// <summary>
    /// Is true is the sort order is ascending
    /// </summary>
    bool IsAsc { get; }
}
