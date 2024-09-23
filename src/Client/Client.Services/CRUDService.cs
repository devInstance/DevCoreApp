using DevInstance.DevCoreApp.Client.Services.Api;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.BlazorUtils.Http;
using DevInstance.BlazorUtils.Services;

namespace DevInstance.DevCoreApp.Client.Services;

public class CRUDService<T> : BaseService, ICRUDService<T> where T : ModelItem
{
    IApiContext<T> Api { get; set; }

    public CRUDService(IApiContext<T> api) 
    {
        Api = api;
    }

    public event DataUpdate<T> OnDataUpdate;

    public async Task<ServiceActionResult<T?>> GetAsync(string id)
    {
        return await HandleWebApiCallAsync(
            async () =>
            {
                var api = Api.Get(id);

                return await api.ExecuteAsync();
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
                var response = await Api.Post(item).ExecuteAsync();
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
                var response = await Api.Put(item.Id, item).ExecuteAsync();
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
                var response = await Api.Delete(item.Id).ExecuteAsync();
                if (response != null)
                {
                    OnDataUpdate?.Invoke(default(T));
                }
                return true;
            }
        );
    }
}
