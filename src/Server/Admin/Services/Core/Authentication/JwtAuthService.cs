using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core.Authentication;
using DevInstance.LogScope;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using DevInstance.BlazorToolkit.Tools;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Core.Authentication;

[BlazorService]
public class JwtAuthService : IJwtAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IQueryRepository _repository;
    private readonly JwtSettings _jwtSettings;
    private readonly IScopeLog _log;

    public JwtAuthService(
        IScopeManager logManager,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IQueryRepository repository,
        IOptions<JwtSettings> jwtSettings)
    {
        _log = logManager.CreateLogger(this);
        _userManager = userManager;
        _signInManager = signInManager;
        _repository = repository;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<ServiceActionResult<JwtLoginResponse>> LoginAsync(JwtLoginRequest request, string? ipAddress, string? userAgent = null)
    {
        using var l = _log.TraceScope();

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            l.I("Login failed: user not found for email");
            await RecordLoginAttemptAsync(null, ipAddress, userAgent, false, "User not found");
            return ServiceActionResult<JwtLoginResponse>.OK(
                JwtLoginResponse.Failure("Invalid email or password."));
        }

        if (user.Status != AccountStatus.Active)
        {
            l.I("Login failed: account not active");
            await RecordLoginAttemptAsync(user.Id, ipAddress, userAgent, false, "Account not active");
            return ServiceActionResult<JwtLoginResponse>.OK(
                JwtLoginResponse.Failure("Account is not active."));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            l.I("Login failed: invalid password");
            await RecordLoginAttemptAsync(user.Id, ipAddress, userAgent, false, "Invalid password");
            return ServiceActionResult<JwtLoginResponse>.OK(
                JwtLoginResponse.Failure("Invalid email or password."));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        var accessToken = GenerateAccessToken(user, roles, expiresAt);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, ipAddress);

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await RecordLoginAttemptAsync(user.Id, ipAddress, userAgent, true, null);

        l.I($"JWT login succeeded for user {user.Id}");

        return ServiceActionResult<JwtLoginResponse>.OK(
            JwtLoginResponse.Success(accessToken, refreshToken, expiresAt));
    }

    public async Task<ServiceActionResult<JwtLoginResponse>> RefreshAsync(string refreshToken, string? ipAddress)
    {
        using var l = _log.TraceScope();

        var tokenHash = HashToken(refreshToken);
        var storedToken = await _repository.GetRefreshTokenQuery(null!)
            .ByTokenHash(tokenHash)
            .Select()
            .FirstOrDefaultAsync();

        if (storedToken == null)
        {
            l.I("Refresh failed: token not found");
            return ServiceActionResult<JwtLoginResponse>.OK(
                JwtLoginResponse.Failure("Invalid refresh token."));
        }

        // Detect reuse of a revoked token — revoke all tokens for the user
        if (storedToken.IsRevoked)
        {
            l.I($"Refresh token reuse detected for user {storedToken.UserId}, revoking all tokens");
            await RevokeAllUserTokensAsync(storedToken.UserId, ipAddress);
            return ServiceActionResult<JwtLoginResponse>.OK(
                JwtLoginResponse.Failure("Token has been revoked. All sessions terminated."));
        }

        if (storedToken.IsExpired)
        {
            l.I("Refresh failed: token expired");
            return ServiceActionResult<JwtLoginResponse>.OK(
                JwtLoginResponse.Failure("Refresh token has expired."));
        }

        var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
        if (user == null || user.Status != AccountStatus.Active)
        {
            l.I("Refresh failed: user not found or inactive");
            return ServiceActionResult<JwtLoginResponse>.OK(
                JwtLoginResponse.Failure("User not found or inactive."));
        }

        // Rotate: revoke old token, issue new one
        var newRawToken = GenerateSecureToken();
        var newTokenHash = HashToken(newRawToken);

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;
        storedToken.ReplacedByTokenHash = newTokenHash;

        var refreshTokenQuery = _repository.GetRefreshTokenQuery(null!);
        var newRefreshToken = refreshTokenQuery.CreateNew();
        newRefreshToken.UserId = storedToken.UserId;
        newRefreshToken.TokenHash = newTokenHash;
        newRefreshToken.ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        newRefreshToken.CreatedAt = DateTime.UtcNow;
        newRefreshToken.CreatedByIp = ipAddress;

        // AddAsync saves in one go, flushing the revoke fields stamped on storedToken above.
        await refreshTokenQuery.AddAsync(newRefreshToken);

        var roles = await _userManager.GetRolesAsync(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        var accessToken = GenerateAccessToken(user, roles, expiresAt);

        l.I($"Token refreshed for user {user.Id}");

        return ServiceActionResult<JwtLoginResponse>.OK(
            JwtLoginResponse.Success(accessToken, newRawToken, expiresAt));
    }

    public async Task<ServiceActionResult<bool>> RevokeAsync(string refreshToken, string? ipAddress)
    {
        using var l = _log.TraceScope();

        var tokenHash = HashToken(refreshToken);
        var refreshTokenQuery = _repository.GetRefreshTokenQuery(null!);
        var storedToken = await refreshTokenQuery
            .ByTokenHash(tokenHash)
            .Select()
            .FirstOrDefaultAsync();

        if (storedToken == null || !storedToken.IsActive)
        {
            l.I("Revoke: token not found or already inactive");
            return ServiceActionResult<bool>.OK(false);
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;
        await refreshTokenQuery.UpdateAsync(storedToken);

        l.I($"Refresh token revoked for user {storedToken.UserId}");

        return ServiceActionResult<bool>.OK(true);
    }

    private string GenerateAccessToken(ApplicationUser user, IList<string> roles, DateTime expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> CreateRefreshTokenAsync(Guid userId, string? ipAddress)
    {
        var rawToken = GenerateSecureToken();
        var tokenHash = HashToken(rawToken);

        var refreshTokenQuery = _repository.GetRefreshTokenQuery(null!);
        var refreshToken = refreshTokenQuery.CreateNew();
        refreshToken.UserId = userId;
        refreshToken.TokenHash = tokenHash;
        refreshToken.ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        refreshToken.CreatedAt = DateTime.UtcNow;
        refreshToken.CreatedByIp = ipAddress;

        await refreshTokenQuery.AddAsync(refreshToken);

        return rawToken;
    }

    private async Task RevokeAllUserTokensAsync(Guid userId, string? ipAddress)
    {
        await _repository.GetRefreshTokenQuery(null!)
            .RevokeAllActiveForUserAsync(userId, ipAddress, DateTime.UtcNow);
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexStringLower(bytes);
    }

    private async Task RecordLoginAttemptAsync(Guid? userId, string? ipAddress, string? userAgent, bool success, string? failureReason)
    {
        if (!userId.HasValue)
            return;

        var loginHistoryQuery = _repository.GetUserLoginHistoryQuery(null!);
        var entry = loginHistoryQuery.CreateNew();
        entry.UserId = userId.Value;
        entry.LoginAt = DateTime.UtcNow;
        entry.IpAddress = ipAddress;
        entry.UserAgent = userAgent;
        entry.Success = success;
        entry.FailureReason = failureReason;

        await loginHistoryQuery.AddAsync(entry);
    }
}
