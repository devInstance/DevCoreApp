using System.Collections;
using System.Globalization;
using System.Reflection;
using DevInstance.BlazorToolkit.Http;
using DevInstance.WebServiceToolkit.Http.Query;

namespace DevInstance.DevCoreApp.Client.Services.Core.Api;

/// <summary>
/// Adds a query model (e.g. <c>ListQuery</c>, <c>EmailLogQuery</c>) to an API call as a correctly
/// encoded query string — the client-side mirror of the server's <c>[QueryModel]</c> binder.
/// </summary>
/// <remarks>
/// BlazorToolkit's URL builder appends <c>name=value.ToString()</c> verbatim: no escaping, and a
/// <see cref="DateTime"/> would be written in the current culture. So values are formatted and
/// escaped here before they reach it:
/// <list type="bullet">
/// <item>strings and every other value are URI-escaped;</item>
/// <item><see cref="DateTime"/> is sent as ISO-8601 UTC with a trailing <c>Z</c> (the API contract);</item>
/// <item>collections are comma-separated, matching the server binder;</item>
/// <item>nulls and empty strings are omitted, so the server applies its defaults.</item>
/// </list>
/// </remarks>
public static class ApiQueryExtensions
{
    public static IApiContext<T> Query<T>(this IApiContext<T> api, object? query)
    {
        if (query == null)
        {
            return api;
        }

        foreach (var property in query.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Computed properties (no setter) are views over other fields, not parameters.
            if (property.SetMethod == null || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var value = Format(property.GetValue(query));
            if (value != null)
            {
                var name = property.GetCustomAttribute<QueryNameAttribute>()?.Name ?? CamelCase(property.Name);
                api.Parameter(name, value);
            }
        }

        return api;
    }

    /// <summary>Formats one query value; null means "leave the parameter out".</summary>
    public static string? Format(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case string s:
                return s.Length == 0 ? null : Uri.EscapeDataString(s);
            case DateTime d:
                return Uri.EscapeDataString(ToUtc(d).ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture));
            case bool b:
                return b ? "true" : "false";
            case Enum e:
                return Uri.EscapeDataString(e.ToString());
            case IEnumerable items:
                var parts = items.Cast<object?>().Select(Format).Where(p => p != null).ToArray();
                return parts.Length == 0 ? null : string.Join(",", parts);
            case IFormattable f:
                return Uri.EscapeDataString(f.ToString(null, CultureInfo.InvariantCulture));
            default:
                return Uri.EscapeDataString(value.ToString() ?? "");
        }
    }

    // Local (browser clock) is converted; Unspecified is taken to be UTC already, as on the server.
    private static DateTime ToUtc(DateTime d) => d.Kind switch
    {
        DateTimeKind.Utc => d,
        DateTimeKind.Local => d.ToUniversalTime(),
        _ => DateTime.SpecifyKind(d, DateTimeKind.Utc)
    };

    private static string CamelCase(string name) => char.ToLowerInvariant(name[0]) + name[1..];
}
