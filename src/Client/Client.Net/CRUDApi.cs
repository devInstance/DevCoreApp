using DevInstance.DevCoreApp.Client.Net.Api;
using DevInstance.DevCoreApp.Client.Net.Utils;
using DevInstance.DevCoreApp.Shared.Model;
using System.Net.Http.Json;

namespace DevInstance.DevCoreApp.Client.Net;

public class CRUDApi<T> : ApiBase, ICRUDApi<T> where T : ModelItem
{
    public string Controller { get; set; }

    public CRUDApi(HttpClient http, string controller) : base(http)
    {
        Controller = controller;
    }

    protected ApiUrlBuilder BuildUrl()
    {
        return ApiUrlBuilder.Create(Controller);
    }

    public async Task<ModelList<T>?> GetItemsAsync(int? top, int? page, ItemFilters? filter, ItemQueries? query, ItemFields? fields)
    {
        string url = BuildUrl().List(top, page, filter, query, fields).ToString();
        return await httpClient.GetFromJsonAsync<ModelList<T>>(url);
    }

    public async Task<T?> GetAsync(string id, ItemFields? fields)
    {
        string url = BuildUrl().Path(id).Query("fields", fields).ToString();
        return await httpClient.GetFromJsonAsync<T>(url);
    }

    public async Task<T?> AddAsync(T payload)
    {
        var result = await httpClient.PostAsJsonAsync(BuildUrl().ToString(), payload);
        result.EnsureSuccessStatusCode();
        return result.Content.ReadFromJsonAsync<T>()?.Result;
    }

    public async Task<T?> UpdateAsync(string id, T payload)
    {
        var result = await httpClient.PutAsJsonAsync(BuildUrl().Path(id).ToString(), payload);
        result.EnsureSuccessStatusCode();
        return result.Content.ReadFromJsonAsync<T>().Result;
    }

    public async Task<T?> RemoveAsync(string id)
    {
        var result = await httpClient.DeleteAsync(BuildUrl().Path(id).ToString());
        result.EnsureSuccessStatusCode();
        return result.Content.ReadFromJsonAsync<T>()?.Result;
    }
}
