using DevInstance.DevCoreApp.Shared.Model;

namespace DevInstance.DevCoreApp.Client.Net.Api;
/// <summary>
/// Declares common Create/Read/Update/Delete api methods.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ICRUDApi<T> where T : ModelItem
{
    Task<ModelList<T>?> GetItemsAsync(int? top, int? page, ItemFilters? filters, ItemQueries? query, ItemFields? fields);

    Task<T?> GetAsync(string id, ItemFields? fields);

    Task<T?> AddAsync(T payload);

    Task<T?> UpdateAsync(string id, T payload);

    Task<T?> RemoveAsync(string id);
}
