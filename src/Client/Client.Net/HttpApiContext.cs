using DevInstance.DevCoreApp.Client.Net.Api;
using DevInstance.DevCoreApp.Client.Net.Utils;
using DevInstance.DevCoreApp.Shared.Model;
using System.Net.Http.Json;

namespace DevInstance.DevCoreApp.Client.Net;

internal class HttpApiContext<T> : IApiContext<T>
{
    private enum ApiMethod
    {
        Get,
        Post,
        Put,
        Delete
    }

    private ApiMethod method;
    private ApiUrlBuilder apiUrlBuilder;
    private T payload;

    public HttpClient Http { get; }

    public HttpApiContext(string url, HttpClient http)
    {
        apiUrlBuilder = ApiUrlBuilder.Create(url);
        Http = http;
    }

    public IApiContext<T> Get(string? id = null)
    {
        method = ApiMethod.Get;
        return this;
    }

    public IApiContext<T> Post(T obj)
    {
        method = ApiMethod.Post;
        payload = obj;
        return this;
    }

    public IApiContext<T> Put(string? id, T obj)
    {
        method = ApiMethod.Put;
        apiUrlBuilder.Path(id);
        payload = obj;
        return this;
    }

    public IApiContext<T> Delete(string? id)
    {
        method = ApiMethod.Delete;
        apiUrlBuilder.Path(id);
        return this;
    }

    public async Task<ModelList<T>?> ListAsync()
    {
        string url = apiUrlBuilder.ToString();
        return await Http.GetFromJsonAsync<ModelList<T>>(url);
    }

    public async Task<T?> ExecuteAsync()
    {
        string url = apiUrlBuilder.ToString();
        switch(method)
        {
            case ApiMethod.Get:
                return await Http.GetFromJsonAsync<T>(url);
            case ApiMethod.Post:
                return await Http.PostAsJsonAsync(url, payload).Result.Content.ReadFromJsonAsync<T>();
            case ApiMethod.Put:
                return await Http.PutAsJsonAsync(url, payload).Result.Content.ReadFromJsonAsync<T>();
            case ApiMethod.Delete:
                return await Http.DeleteAsync(url).Result.Content.ReadFromJsonAsync<T>();
            default:
                return default;
        }
    }

    public IApiContext<T> Top(int top)
    {
        apiUrlBuilder.Query("top", top);
        return this;
    }

    public IApiContext<T> Page(int page)
    {
        apiUrlBuilder.Query("page", page);
        return this;
    }

    public IApiContext<T> Search(string key)
    {
        apiUrlBuilder.Query("search", key);
        return this;
    }

    public IApiContext<T> Sort(string key, bool isAsc)
    {
        apiUrlBuilder.Query("sortBy", key);
        apiUrlBuilder.Query("asc", isAsc);
        return this;
    }
}
