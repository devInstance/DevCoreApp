using DevInstance.BlazorToolkit.Http;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Services.Wasm;
using DevInstance.LogScope;

namespace DevInstance.DevCoreApp.Client.Services.Core.Api;

/// <summary>
/// Base for client services that call <c>/api</c>. Pages never see HTTP: they call a service
/// method and get a <see cref="ServiceActionResult{T}"/>, exactly as they did against the
/// server-side services — so page code runs through <c>IServiceExecutionHost</c> unchanged.
/// </summary>
public abstract class ApiServiceBase
{
    private readonly IHttpApiContextFactory apiFactory;

    protected ApiServiceBase(IHttpApiContextFactory apiFactory, IScopeManager logManager)
    {
        this.apiFactory = apiFactory;
        Log = logManager.CreateLogger(this);
    }

    protected IScopeLog Log { get; }

    /// <summary>A request builder for <paramref name="path"/> (relative, e.g. <c>"api/users"</c>).</summary>
    protected IApiContext<T> Api<T>(string path) => apiFactory.Create<T>(ApiClient.HttpClientName, path);

    /// <summary>
    /// Runs one API call and converts the outcome to a <see cref="ServiceActionResult{T}"/>:
    /// the server's <c>ServiceActionError</c> body on failure, <c>IsAuthorized = false</c> on 401.
    /// </summary>
    protected Task<ServiceActionResult<T>> CallAsync<T>(Func<Task<T?>> call)
    {
        return ServiceUtils.HandleWebApiCallAsync<T>(async _ => (await call())!, Log);
    }
}
