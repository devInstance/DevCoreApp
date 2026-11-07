using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Utils.Core;
using DevInstance.WebServiceToolkit.Common.Model;

namespace DevInstance.DevCoreApp.Server.Services.Core.Search;

/// <summary>
/// Computes <see cref="PagedList{T}.Highlights"/>: where a search term occurs in the fields a list page
/// shows, as ranges into the view-model values. Markup never goes into the DTO strings — clients
/// render the ranges themselves.
/// </summary>
/// <remarks>
/// The matching here must agree with the query's <c>Search()</c>: highlight a field only if the query
/// searches it, and match the way the query does. Configure it next to the <c>Search()</c> call in the
/// service's list method, so the two are read and changed together. When a query moves to a different
/// matching model (case-insensitive, full-text), its highlighter changes in the same commit.
/// </remarks>
public static class SearchHighlighter
{
    /// <summary>
    /// Adds highlights to <paramref name="list"/> for <paramref name="search"/>; a no-op when there is no
    /// search. Returns the same list for chaining.
    /// </summary>
    public static PagedList<T> WithHighlights<T>(this PagedList<T> list, string? search,
        Action<SearchHighlightBuilder<T>> configure) where T : IModelItem
    {
        if (list?.Items == null || string.IsNullOrEmpty(search))
            return list!;

        var builder = new SearchHighlightBuilder<T>(search);
        configure(builder);

        var highlights = new Dictionary<string, Dictionary<string, HighlightRange[]>>();
        foreach (var item in list.Items)
        {
            if (item?.Id == null)
                continue;

            var fields = builder.Compute(item);
            if (fields.Count > 0)
                highlights[item.Id] = fields;
        }

        list.Highlights = highlights.Count > 0 ? highlights : null;
        return list;
    }

    /// <summary>Every non-overlapping occurrence of <paramref name="term"/> in <paramref name="text"/>.</summary>
    public static HighlightRange[] FindText(string? text, string term, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(term))
            return Array.Empty<HighlightRange>();

        var ranges = new List<HighlightRange>();
        var index = text.IndexOf(term, 0, comparison);
        while (index >= 0)
        {
            ranges.Add(new HighlightRange(index, term.Length));
            var next = index + term.Length;
            index = next < text.Length ? text.IndexOf(term, next, comparison) : -1;
        }
        return ranges.ToArray();
    }

    /// <summary>
    /// Every occurrence of the digit sequence <paramref name="digits"/> in <paramref name="text"/>,
    /// ignoring everything that is not a digit — "5551234" inside "(555) 123-4567". Each range spans
    /// from the first matched digit to the last, separators included.
    /// </summary>
    public static HighlightRange[] FindDigits(string? text, string digits)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(digits))
            return Array.Empty<HighlightRange>();

        var positions = new List<int>();
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsAsciiDigit(text[i]))
                positions.Add(i);
        }

        var textDigits = string.Concat(positions.Select(p => text[p]));
        var ranges = new List<HighlightRange>();
        var index = textDigits.IndexOf(digits, StringComparison.Ordinal);
        while (index >= 0)
        {
            var start = positions[index];
            var end = positions[index + digits.Length - 1] + 1;
            ranges.Add(new HighlightRange(start, end - start));
            var next = index + digits.Length;
            index = next < textDigits.Length ? textDigits.IndexOf(digits, next, StringComparison.Ordinal) : -1;
        }
        return ranges.ToArray();
    }
}

/// <summary>Declares which view-model fields a list highlights and how each is matched.</summary>
public sealed class SearchHighlightBuilder<T> where T : IModelItem
{
    private readonly List<(string Field, Func<T, string?> Selector, Func<string, HighlightRange[]> Find)> fields = new();

    internal SearchHighlightBuilder(string search)
    {
        Search = search;
    }

    /// <summary>The term being highlighted, as the service received it.</summary>
    public string Search { get; }

    /// <summary>
    /// How <see cref="Text"/> fields compare. Case-insensitive by default, like the queries'
    /// <c>Search()</c> (<c>column.ToLower().Contains(term)</c>); change it only together with the query.
    /// </summary>
    public StringComparison Comparison { get; set; } = StringComparison.OrdinalIgnoreCase;

    /// <summary>Highlights occurrences of the term in a text field.</summary>
    /// <param name="field">The DTO property name — use <c>nameof</c>.</param>
    public SearchHighlightBuilder<T> Text(string field, Func<T, string?> selector) =>
        Custom(field, selector, value => SearchHighlighter.FindText(value, Search, Comparison));

    /// <summary>
    /// Highlights a formatted phone number: the term as typed, plus its digits ignoring separators
    /// once it has at least <paramref name="minDigits"/> of them (the query's own guard).
    /// </summary>
    public SearchHighlightBuilder<T> Phone(string field, Func<T, string?> selector, int minDigits = 3)
    {
        var digits = Search.PhoneDigits();
        return Custom(field, selector, value =>
        {
            var text = SearchHighlighter.FindText(value, Search, Comparison);
            // The literal and the digit match are often the same span ("123-45"); report it once.
            return digits.Length >= minDigits
                ? text.Concat(SearchHighlighter.FindDigits(value, digits))
                      .DistinctBy(r => (r.Start, r.Length))
                      .OrderBy(r => r.Start)
                      .ToArray()
                : text;
        });
    }

    /// <summary>Highlights a field with a caller-supplied matcher.</summary>
    public SearchHighlightBuilder<T> Custom(string field, Func<T, string?> selector, Func<string, HighlightRange[]> find)
    {
        fields.Add((field, selector, find));
        return this;
    }

    internal Dictionary<string, HighlightRange[]> Compute(T item)
    {
        var result = new Dictionary<string, HighlightRange[]>();
        foreach (var (field, selector, find) in fields)
        {
            var value = selector(item);
            if (string.IsNullOrEmpty(value))
                continue;

            var ranges = find(value);
            if (ranges.Length > 0)
                result[field] = ranges;
        }
        return result;
    }
}
