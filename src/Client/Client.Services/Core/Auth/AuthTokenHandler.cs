using System.Net;
using System.Net.Http.Headers;

namespace DevInstance.DevCoreApp.Client.Services.Core.Auth;

/// <summary>
/// Attaches the bearer token to every <c>/api</c> request. Refreshes first when the token is about
/// to expire, and on a 401 refreshes once and replays the request.
/// </summary>
public sealed class AuthTokenHandler : DelegatingHandler
{
    /// <summary>Refresh this long before expiry so a request does not race the deadline.</summary>
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromSeconds(30);

    private readonly AuthTokenStore store;
    private readonly TokenRefresher refresher;

    public AuthTokenHandler(AuthTokenStore store, TokenRefresher refresher)
    {
        this.store = store;
        this.refresher = refresher;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tokens = await store.GetAsync();
        if (tokens == null)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var accessToken = tokens.AccessToken;
        if (tokens.ExpiresAtUtc - RefreshMargin <= DateTime.UtcNow)
        {
            accessToken = await refresher.RefreshAsync(accessToken);
        }

        // The body may have to be sent twice; buffer it before the first attempt consumes it.
        if (request.Content != null)
        {
            await request.Content.LoadIntoBufferAsync(cancellationToken);
        }

        SetBearer(request, accessToken);
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized || accessToken == null)
        {
            return response;
        }

        var refreshed = await refresher.RefreshAsync(accessToken);
        if (refreshed == null)
        {
            return response;
        }

        response.Dispose();
        var retry = await CloneAsync(request);
        SetBearer(retry, refreshed);
        return await base.SendAsync(retry, cancellationToken);
    }

    private static void SetBearer(HttpRequestMessage request, string? accessToken) =>
        request.Headers.Authorization = accessToken == null ? null : new AuthenticationHeaderValue("Bearer", accessToken);

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content != null)
        {
            var content = new ByteArrayContent(await request.Content.ReadAsByteArrayAsync());
            foreach (var header in request.Content.Headers)
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            clone.Content = content;
        }

        return clone;
    }
}
