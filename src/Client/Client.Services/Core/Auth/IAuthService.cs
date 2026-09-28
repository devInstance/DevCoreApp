using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core.Authentication;

namespace DevInstance.DevCoreApp.Client.Services.Core.Auth;

/// <summary>Sign-in and sign-out against <c>api/auth</c> (JWT).</summary>
public interface IAuthService
{
    /// <summary>Signs in; on success the authentication state changes and <c>api/me</c> is loaded.</summary>
    Task<ServiceActionResult<bool>> LoginAsync(JwtLoginRequest request);

    /// <summary>Revokes the refresh token on the server (best effort) and forgets the session.</summary>
    Task<ServiceActionResult<bool>> LogoutAsync();
}
