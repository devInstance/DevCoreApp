using DevInstance.DevCoreApp.Server.Admin.Services.Core.Authentication;
using DevInstance.DevCoreApp.Shared.Model.Core.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Core.Controllers;

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IJwtAuthService _jwtAuthService;

    public AuthController(IJwtAuthService jwtAuthService)
    {
        _jwtAuthService = jwtAuthService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<JwtLoginResponse>> LoginAsync([FromBody] JwtLoginRequest request)
    {
        // IP and User-Agent are read here because they are transport facts, not request data:
        // the refresh-token record stores them for audit and reuse detection, and services
        // must not depend on HttpContext.
        return HandleServiceAsync(() => _jwtAuthService.LoginAsync(request, ClientIp, UserAgent));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<JwtLoginResponse>> RefreshAsync([FromBody] RefreshTokenRequest request)
    {
        // IP: see LoginAsync.
        return HandleServiceAsync(() => _jwtAuthService.RefreshAsync(request.RefreshToken, ClientIp));
    }

    [Authorize]
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<bool>> RevokeAsync([FromBody] RefreshTokenRequest request)
    {
        // IP: see LoginAsync.
        return HandleServiceAsync(() => _jwtAuthService.RevokeAsync(request.RefreshToken, ClientIp));
    }

    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? UserAgent => HttpContext.Request.Headers.UserAgent.FirstOrDefault();
}
