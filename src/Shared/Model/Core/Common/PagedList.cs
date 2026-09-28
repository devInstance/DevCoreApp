using System;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;

namespace DevInstance.DevCoreApp.Shared.Model.Core.Common;

/// <summary>
/// The one paginated list type used by services, the API and the clients.
/// Replaces WebServiceToolkit's obsolete <c>ModelList&lt;T&gt;</c> and serializes to the same JSON.
/// </summary>
public class PagedList<T> : IModelList<T>
{
    public int TotalCount { get; set; }
    public int PagesCount { get; set; }
    public int Page { get; set; }
    public int Count { get; set; }

    /// <summary>Sort criteria: "+Name" ascending, "-Name" descending, in priority order.</summary>
    public string[] SortOrder { get; set; } = Array.Empty<string>();

    public string Search { get; set; }
    public T[] Items { get; set; } = Array.Empty<T>();

    public static PagedList<T> Empty() => new();

    public static bool IsEmpty(PagedList<T> list) => list?.Items == null || list.Items.Length == 0;
}

/// <summary>
/// Factory for <see cref="PagedList{T}"/> that keeps generic type inference at call sites.
/// </summary>
public static class PagedList
{
    /// <summary>
    /// Builds a page and computes <c>PagesCount</c>; see WebServiceToolkit
    /// <see cref="ModelListResult"/> for the parameter semantics.
    /// </summary>
    public static PagedList<T> Create<T>(T[] items, int? totalCount = null, int? top = null, int? page = null,
        string[] sortOrder = null, string search = null, bool useSearchMarkup = false)
    {
        return ModelListResult.CreateList<PagedList<T>, T>(items, totalCount, top, page, sortOrder, search, useSearchMarkup);
    }

    public static PagedList<T> Single<T>(T item) => Create(new[] { item });

    /// <summary>Primary sort field without its "+"/"-" direction prefix, or null when unsorted.</summary>
    public static string SortField<T>(this PagedList<T> list)
    {
        var first = list?.SortOrder?.Length > 0 ? list.SortOrder[0] : null;
        return string.IsNullOrEmpty(first) ? null : first.TrimStart('+', '-');
    }

    /// <summary>True unless the primary sort field is descending ("-Name").</summary>
    public static bool IsAscending<T>(this PagedList<T> list)
    {
        var first = list?.SortOrder?.Length > 0 ? list.SortOrder[0] : null;
        return first == null || !first.StartsWith("-");
    }
}
