using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Auth;
using DevInstance.DevCoreApp.Shared.Model.Core.Authentication;

namespace DevInstance.DevCoreApp.Client.Services.Mocks.Core.Auth;

/// <summary>
/// Any email and password sign in. Stores a dummy session so the real
/// ApiAuthenticationStateProvider runs unchanged (it then reads the user from IMeService's mock).
/// </summary>
[BlazorServiceMock]
public class AuthServiceMock : IAuthService
{
    private readonly AuthTokenStore store;

    public AuthServiceMock(AuthTokenStore store) => this.store = store;

    public async Task<ServiceActionResult<bool>> LoginAsync(JwtLoginRequest request)
    {
        await Task.Delay(300);
        await store.SetAsync(new AuthTokens("mock-access", "mock-refresh", DateTime.UtcNow.AddYears(1)), signIn: true);
        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<bool>> LogoutAsync()
    {
        await store.ClearAsync();
        return ServiceActionResult<bool>.OK(true);
    }
}
