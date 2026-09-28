namespace DevInstance.DevCoreApp.Client.Services.Core.Api;

/// <summary>
/// Names and options of the HTTP clients the WASM apps use to reach the server.
/// </summary>
public static class ApiClient
{
    /// <summary>Client for every <c>/api</c> call: base address = API origin, bearer token attached by <see cref="Auth.AuthTokenHandler"/>.</summary>
    public const string HttpClientName = "DevCoreApp.Api";

    /// <summary>
    /// Client for <c>api/auth</c> login and refresh. It has no <see cref="Auth.AuthTokenHandler"/>,
    /// so a token refresh can never trigger another refresh.
    /// </summary>
    public const string AuthHttpClientName = "DevCoreApp.Auth";
}

/// <summary>Where the API lives. Same origin as the app unless configured (<c>ApiBaseUrl</c>).</summary>
public sealed class ApiClientOptions
{
    public ApiClientOptions(Uri baseAddress)
    {
        // A trailing slash makes relative paths ("api/me") resolve under the base, not beside it.
        BaseAddress = baseAddress.AbsoluteUri.EndsWith('/') ? baseAddress : new Uri(baseAddress.AbsoluteUri + "/");
    }

    public Uri BaseAddress { get; }
}
