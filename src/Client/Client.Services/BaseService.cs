using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
namespace DevInstance.DevCoreApp.Client.Services;

public delegate void ResultHandler<T>(T result);

public class BaseService
{
    public IScopeLog? Log { get; protected set; } = null;

    public delegate Task<T> WebApiHandlerAsync<T>();

    protected async Task<ServiceActionResult<T>> HandleWebApiCallAsync<T>(WebApiHandlerAsync<T> handler)
    {
        try
        {
            return new ServiceActionResult<T>
            {
                Result = await handler(),
                Success = true
            };
        }
        catch (Exception ex)
        {
            return new ServiceActionResult<T>
            {
                Success = false,
                ErrorMessage = ex.Message,
                Result = default(T)!
            };
        }
    }

}
