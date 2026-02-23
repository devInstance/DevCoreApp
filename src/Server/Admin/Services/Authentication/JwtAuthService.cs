using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Authentication;
using DevInstance.LogScope;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using DevInstance.BlazorToolkit.Tools;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Authentication;

[BlazorService]
public class JwtAuthService : IJwtAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly JwtSettings _jwtSettings;
    private readonly IScopeLog _log;

    public JwtAuthService(
        IScopeManager logManager,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext dbContext,
        IOptions<JwtSettings> jwtSettings)
    {
        _log = logManager.CreateLogger(this);
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<ServiceActionResult<JwtLoginResponse>> LoginAsync(JwtLoginRequest request, string? ipAddress)
    {
        using var l = _log.TraceScope();

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            l.I("Login failed: user not found for email");
            return ServiceActionResult<JwtLoginResponse>.OK(
                JwtLoginResponse.Failure("Invalid email or password."));
        }

        if (user.Status != AccountStatus.Active)
        {
            l.I("Login failed: account not active");
            return ServiceActionResult<JwtLoginResponse>.OK(
                JwtLoginResponse.Failure("Account is not active."));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            l.I("Login failed: invalid password");
            return ServiceActionResult<JwtLoginResponse>.OK(
                JwtLoginResponse.Failure("Invalid email or password."));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        var accessToken = GenerateAccessToken(user, roles, expiresAt);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, ipAddress);

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        l.I($"JWT login succeeded for user {user.Id}");

        return ServiceActionResult<JwtLoginResponse>.OK(
            JwtLoginResponse.Success(accessToken, refreshToken, expiresAt));
    }

    public async Task<ServiceActionResult<JwtLoginResponse>> RefreshAsync(string refreshToken, string? ipAddress)
    {
        using var l = _log.TraceScope();

        var tokenHash = HashToken(refreshToken);
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

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

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = storedToken.UserId,
            TokenHash = newTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync();

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
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (storedToken == null || !storedToken.IsActive)
        {
            l.I("Revoke: token not found or already inactive");
            return ServiceActionResult<bool>.OK(false);
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;
        await _dbContext.SaveChangesAsync();

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

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        return rawToken;
    }

    private async Task RevokeAllUserTokensAsync(Guid userId, string? ipAddress)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
        }

        await _dbContext.SaveChangesAsync();
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
}
