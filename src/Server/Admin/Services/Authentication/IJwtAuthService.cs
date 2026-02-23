using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Authentication;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Authentication;

public interface IJwtAuthService
{
    Task<ServiceActionResult<JwtLoginResponse>> LoginAsync(JwtLoginRequest request, string? ipAddress);
    Task<ServiceActionResult<JwtLoginResponse>> RefreshAsync(string refreshToken, string? ipAddress);
    Task<ServiceActionResult<bool>> RevokeAsync(string refreshToken, string? ipAddress);
}
