using DevInstance.DevCoreApp.Shared.Model;
using System.Text;

namespace DevInstance.DevCoreApp.Client.Net.Utils;

public class ApiUrlBuilder
{
    StringBuilder result;

    protected Dictionary<string, object> _filters = new Dictionary<string, object>();
    protected List<string> _path = new List<string>();

    private ApiUrlBuilder(string controller)
    {
        result = new StringBuilder(controller);
    }

    public static ApiUrlBuilder Create(string controller) 
    {
        return new ApiUrlBuilder(controller);
    }

    public ApiUrlBuilder Query(string name, object? value)
    {
        if (value != null)
        {
            _filters.Add(name, value);
        }
        return this;
    }

    public ApiUrlBuilder Path(string? value)
    {
        if (value != null)
        {
            _path.Add(value);
        }
        return this;
    }

    public ApiUrlBuilder List(int? top, int? page, ItemFilters? filter, ItemQueries? query, ItemFields? fields)
    {
        if (top.HasValue)
        {
            Query("top", top);
        }
        if (page.HasValue)
        {
            Query("page", page);
        }
        if (filter != null)
        {
            Query("filter", filter);
        }
        if (fields != null)
        {
            Query("fields", fields);
        }
        if (query != null)
        {
            foreach (var item in query)
            {
                Query(item.Key, item.Value);
            }
        }

        return this;
    }

    public override string ToString()
    {
        foreach (var item in _path)
        {
            result.Append("/").Append(item);
        }

        bool hasQuery = false;
        foreach (var pair in _filters)
        {
            if (hasQuery)
            {
                result.Append("&");
            }
            else
            {
                result.Append("?");
            }
            result.Append(pair.Key).Append("=").Append(pair.Value);

            hasQuery = true;
        }

        return result.ToString();
    }
}
