using DevInstance.LogScope;
using System.Net;

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
                Success = true,
                IsAuthorized = true,
            };
        }
        catch (HttpRequestException ex)
        {
            return new ServiceActionResult<T>
            {
                Success = false,
                Errors = new ServiceActionError[]
                {
                    new ServiceActionError
                    {
                        //TODO: figure out how to deliver field name for the conflict
                        Message = ex.Message
                    }
                },
                IsAuthorized = !(ex.StatusCode == HttpStatusCode.Unauthorized),
                Result = default(T)!
            };

        }
        catch (Exception ex)
        {
            return new ServiceActionResult<T>
            {
                Success = false,
                Errors = new ServiceActionError[] { new ServiceActionError { Message = ex.Message } },
                Result = default(T)!
            };
        }
    }
}
