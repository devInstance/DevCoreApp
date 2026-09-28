using System;
using System.ComponentModel;
using DevInstance.WebServiceToolkit.Http.Query;

namespace DevInstance.DevCoreApp.Shared.Model.Core.Common;

/// <summary>
/// Query-string model shared by every paged <c>/api</c> list endpoint:
/// <c>?top=20&amp;page=0&amp;sortBy=-CreateDate,Name&amp;search=abc</c>.
/// Feature queries derive from it and add their filters. Bound by WebServiceToolkit's
/// <c>[QueryModel]</c> binder on the server; the clients build the same query string.
/// </summary>
/// <remarks>
/// Sort fields: <c>Name</c> is ascending, <c>-Name</c> descending, comma-separated in priority
/// order. There is no "+" prefix — it decodes to a space in a query string.
/// Dates in derived queries are UTC; see <see cref="Utc"/>.
/// </remarks>
[QueryModel]
public class ListQuery
{
    [DefaultValue(20)]
    public int Top { get; set; } = 20;

    [DefaultValue(0)]
    public int Page { get; set; }

    public string[] SortBy { get; set; }

    public string Search { get; set; }

    /// <summary>Primary sort field without its "-" prefix, for services that take field + direction.</summary>
    public string SortField => SortBy?.Length > 0 && !string.IsNullOrEmpty(SortBy[0]) ? SortBy[0].TrimStart('-') : null;

    /// <summary>Direction of <see cref="SortField"/>; null when unsorted.</summary>
    public bool? IsAsc => SortField == null ? null : !SortBy[0].StartsWith("-");

    /// <summary>
    /// Normalizes a bound date to UTC. The binder yields <c>Kind=Utc</c> for "…Z" values,
    /// <c>Local</c> for values with an offset (converted, so the instant is right) and
    /// <c>Unspecified</c> for values with neither — which the API contract defines as UTC.
    /// </summary>
    protected static DateTime? Utc(DateTime? value) => value?.Kind switch
    {
        null => null,
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.Value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
    };
}

/// <summary>
/// A <see cref="ListQuery"/> with a UTC date range, used by the log-style endpoints.
/// </summary>
[QueryModel]
public class DateRangeListQuery : ListQuery
{
    private DateTime? startDate;
    private DateTime? endDate;

    public DateTime? StartDate { get => startDate; set => startDate = Utc(value); }
    public DateTime? EndDate { get => endDate; set => endDate = Utc(value); }
}
