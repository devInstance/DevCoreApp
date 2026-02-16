using DevInstance.BlazorToolkit.Services;
using DevInstance.WebServiceToolkit.Common.Model;

namespace DevInstance.DevCoreApp.Server.Admin.Services;

public interface ICRUDService<T> where T : ModelItem
{
    /// <summary>
    /// Retrieves a paginated list of models that match the specified search and sorting criteria.
    /// </summary>
    /// <param name="top">The maximum number of items to retrieve. If null, all available items are returned.</param>
    /// <param name="page">The page number to retrieve, starting from 1. If null, the first page is returned.</param>
    /// <param name="sortBy">An array of property names used to sort the results. Use "+"/"-" for ascending or descending order. If null or empty, the default sort order is applied.</param>
    /// <param name="search">A search term used to filter the results. If null or empty, no filtering is applied.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a ServiceActionResult with a
    /// ModelList of type T that holds the retrieved models. If this is a search result, the items fields should include <mark>...</mark> to highlight matched values.</returns>
    Task<ServiceActionResult<ModelList<T>>> GetListAsync(int? top, int? page, string[] sortBy, string search);

    Task<ServiceActionResult<T>> GetAsync(string id);

    Task<ServiceActionResult<T>> AddAsync(T item);

    Task<ServiceActionResult<T>> UpdateAsync(string id, T item);

    Task<ServiceActionResult<T>> DeleteAsync(string id);
}
