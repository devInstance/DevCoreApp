using DevInstance.BlazorToolkit.Http;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Shared.Model.Core.Authentication;
using DevInstance.LogScope;

namespace DevInstance.DevCoreApp.Client.Services.Core.Auth;

[BlazorService]
public class AuthService : IAuthService
{
    private readonly IHttpApiContextFactory apiFactory;
    private readonly AuthTokenStore store;
    private readonly IScopeLog log;

    public AuthService(IHttpApiContextFactory apiFactory, AuthTokenStore store, IScopeManager logManager)
    {
        this.apiFactory = apiFactory;
        this.store = store;
        log = logManager.CreateLogger(this);
    }

    public async Task<ServiceActionResult<bool>> LoginAsync(JwtLoginRequest request)
    {
        using var l = log.TraceScope();

        // The auth client: login must never carry (or try to refresh) a stale session's token.
        var result = await BlazorToolkit.Services.Wasm.ServiceUtils.HandleWebApiCallAsync(async _ =>
            await apiFactory.Create<JwtLoginResponse>(ApiClient.AuthHttpClientName, "api/auth/login")
                .Post<JwtLoginRequest>(request)
                .ExecuteAsync(), l);

        if (!result.Success)
        {
            return ServiceActionResult<bool>.Failed(result.Errors?.FirstOrDefault()?.Message ?? "Sign-in failed.");
        }

        // api/auth/login reports a rejected sign-in as 200 + Succeeded=false.
        var response = result.Result;
        if (response?.Succeeded != true || string.IsNullOrEmpty(response.AccessToken) || string.IsNullOrEmpty(response.RefreshToken))
        {
            return ServiceActionResult<bool>.Failed(response?.ErrorMessage ?? "Invalid email or password.");
        }

        await store.SetAsync(TokenRefresher.ToTokens(response), signIn: true);
        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<bool>> LogoutAsync()
    {
        using var l = log.TraceScope();

        var tokens = await store.GetAsync();
        if (tokens != null)
        {
            // Best effort: a failed revoke must not keep the user signed in on this device.
            var revoke = await BlazorToolkit.Services.Wasm.ServiceUtils.HandleWebApiCallAsync(async _ =>
                await apiFactory.Create<bool>(ApiClient.HttpClientName, "api/auth/revoke")
                    .Post<RefreshTokenRequest>(new RefreshTokenRequest { RefreshToken = tokens.RefreshToken })
                    .ExecuteAsync(), l);
            if (!revoke.Success)
            {
                l.W("Refresh token revoke failed; signing out locally anyway.");
            }
        }

        await store.ClearAsync();
        return ServiceActionResult<bool>.OK(true);
    }
}
