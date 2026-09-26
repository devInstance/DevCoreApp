using System.Net.Http.Json;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Shared.Model.Core.Authentication;

namespace DevInstance.DevCoreApp.Client.Services.Core.Auth;

/// <summary>
/// Exchanges the refresh token for a new pair (<c>POST api/auth/refresh</c>). Single-flight: when
/// several requests hit 401 together, one refresh runs and the others reuse its result — the
/// server rotates refresh tokens and treats reuse of an old one as theft, revoking every session.
/// </summary>
public sealed class TokenRefresher
{
    private readonly IHttpClientFactory httpFactory;
    private readonly AuthTokenStore store;
    private readonly SemaphoreSlim gate = new(1, 1);

    public TokenRefresher(IHttpClientFactory httpFactory, AuthTokenStore store)
    {
        this.httpFactory = httpFactory;
        this.store = store;
    }

    /// <summary>
    /// Returns a usable access token, or null when the session is gone (refresh rejected → signed
    /// out). <paramref name="staleAccessToken"/> is the token that just failed; if another caller
    /// already replaced it, that newer token is returned without a second refresh.
    /// </summary>
    public async Task<string?> RefreshAsync(string? staleAccessToken)
    {
        await gate.WaitAsync();
        try
        {
            var current = await store.GetAsync();
            if (current == null)
            {
                return null;
            }

            if (staleAccessToken != null && current.AccessToken != staleAccessToken)
            {
                return current.AccessToken;
            }

            HttpResponseMessage response;
            try
            {
                var client = httpFactory.CreateClient(ApiClient.AuthHttpClientName);
                response = await client.PostAsJsonAsync("api/auth/refresh", new RefreshTokenRequest { RefreshToken = current.RefreshToken });
            }
            catch (HttpRequestException)
            {
                // Offline or server down: keep the session, fail only this request.
                return null;
            }

            var body = response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<JwtLoginResponse>() : null;
            if (body?.Succeeded != true || string.IsNullOrEmpty(body.AccessToken) || string.IsNullOrEmpty(body.RefreshToken))
            {
                await store.ClearAsync();
                return null;
            }

            await store.SetAsync(ToTokens(body), signIn: false);
            return body.AccessToken;
        }
        finally
        {
            gate.Release();
        }
    }

    internal static AuthTokens ToTokens(JwtLoginResponse response) => new(
        response.AccessToken!,
        response.RefreshToken!,
        // ExpiresAt is UTC on the wire; a missing value is treated as already due for refresh.
        response.ExpiresAt?.ToUniversalTime() ?? DateTime.UtcNow);
}
