using DevInstance.DevCoreApp.Client.Net.Api;
using DevInstance.DevCoreApp.Client.Services.Api;
using DevInstance.DevCoreApp.Shared.Model;

namespace DevInstance.DevCoreApp.Client.Services;

public class CRUDService<T> : BaseService, ICRUDService<T> where T : ModelItem
{
    ICRUDApi<T> CRUDApi { get; set; }

    public CRUDService(ICRUDApi<T> cRUDApi) 
    {
        CRUDApi = cRUDApi;
    }

    public event DataUpdate<T> OnDataUpdate;

    public async Task<ServiceActionResult<ModelList<T>?>> GetItemsAsync(int? top, int? page, ItemFilters? filter, ItemFields? fields, string? search)
    {
        return await HandleWebApiCallAsync(
            async () =>
            {
                return await CRUDApi.GetItemsAsync(top, page, filter, new ItemQueries { { ItemQuery.Search, search } }, fields);
            }
        );
    }

    public async Task<ServiceActionResult<T?>> GetAsync(string id)
    {
        return await HandleWebApiCallAsync(
            async () =>
            {
                return await CRUDApi.GetAsync(id, new ItemFields { ItemField.All });
            }
        );
    }

    public void SetDataUpdate(T item)
    {
        OnDataUpdate?.Invoke(item);
    }
    
    public async Task<ServiceActionResult<T?>> AddNewAsync(T item)
    {
        return await HandleWebApiCallAsync(
            async () =>
            {
                var response = await CRUDApi.AddAsync(item);
                if (response != null)
                {
                    OnDataUpdate?.Invoke(response);
                }
                return response;
            }
        );
    }

    public async Task<ServiceActionResult<T?>> UpdateAsync(T item)
    {
        return await HandleWebApiCallAsync(
            async () =>
            {
                var response = await CRUDApi.UpdateAsync(item.Id, item);
                if (response != null)
                {
                    OnDataUpdate?.Invoke(response);
                }
                return response;
            }
        );
    }

    public async Task<ServiceActionResult<bool>> RemoveAsync(T item)
    {
        return await HandleWebApiCallAsync(
            async () =>
            {
                var response = await CRUDApi.RemoveAsync(item.Id);
                if (response != null)
                {
                    OnDataUpdate?.Invoke(default(T));
                }
                return true;
            }
        );
    }
}
