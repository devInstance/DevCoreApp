using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Shared.Model.Authentication;
using DevInstance.WebServiceToolkit.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static DevInstance.WebServiceToolkit.Controllers.ControllerUtils;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
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
    public async Task<ActionResult<JwtLoginResponse>> LoginAsync([FromBody] JwtLoginRequest request)
    {
        return await this.HandleWebRequestAsync<JwtLoginResponse>(async () =>
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.FirstOrDefault();
            var result = await _jwtAuthService.LoginAsync(request, ipAddress, userAgent);
            return Ok(result.Result);
        });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JwtLoginResponse>> RefreshAsync([FromBody] RefreshTokenRequest request)
    {
        return await this.HandleWebRequestAsync<JwtLoginResponse>(async () =>
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _jwtAuthService.RefreshAsync(request.RefreshToken, ipAddress);
            return Ok(result.Result);
        });
    }

    [Authorize]
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<bool>> RevokeAsync([FromBody] RefreshTokenRequest request)
    {
        return await this.HandleWebRequestAsync<bool>(async () =>
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _jwtAuthService.RevokeAsync(request.RefreshToken, ipAddress);
            return Ok(result.Result);
        });
    }
}
