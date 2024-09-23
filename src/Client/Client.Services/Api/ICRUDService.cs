using DevInstance.DevCoreApp.Shared.Model;

namespace DevInstance.DevCoreApp.Client.Services.Api;

public delegate Task DataUpdate<T>(T item);

public interface ICRUDService<T> where T : ModelItem
{
    event DataUpdate<T> OnDataUpdate;

    //Task<ServiceActionResult<ModelList<T>?>> GetItemsAsync(int? top, int? page, ItemFilters? filters, ItemFields? fields, string? search);

    Task<ServiceActionResult<T?>> GetAsync(string id);
    
    Task<ServiceActionResult<T?>> AddNewAsync(T item);

    Task<ServiceActionResult<T?>> UpdateAsync(T item);

    Task<ServiceActionResult<bool>> RemoveAsync(T item);
}
