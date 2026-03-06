using DevInstance.DevCoreApp.Server.Database.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Identity;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    private readonly ApplicationDbContext _db;
    private readonly IServiceProvider _serviceProvider;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ApplicationDbContext db,
        IServiceProvider serviceProvider)
        : base(options, logger, encoder)
    {
        _db = db;
        _serviceProvider = serviceProvider;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeader))
            return AuthenticateResult.NoResult();

        var apiKeyValue = apiKeyHeader.ToString();
        if (string.IsNullOrEmpty(apiKeyValue))
            return AuthenticateResult.NoResult();

        // Hash the key and look it up
        var keyHash = HashKey(apiKeyValue);

        var apiKey = await _db.ApiKeys
            .Include(ak => ak.CreatedBy)
            .FirstOrDefaultAsync(ak => ak.KeyHash == keyHash);

        if (apiKey == null)
            return AuthenticateResult.Fail("Invalid API key.");

        if (!apiKey.IsActive)
            return AuthenticateResult.Fail("API key is inactive.");

        if (apiKey.IsRevoked)
            return AuthenticateResult.Fail("API key has been revoked.");

        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
            return AuthenticateResult.Fail("API key has expired.");

        // Resolve the ApplicationUser for this key's creator
        var userManager = _serviceProvider.GetRequiredService<UserManager<Database.Core.Models.ApplicationUser>>();
        var appUser = await userManager.FindByIdAsync(apiKey.CreatedBy.ApplicationUserId.ToString());

        if (appUser == null)
            return AuthenticateResult.Fail("API key owner not found.");

        // Build claims identity
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, appUser.Id.ToString()),
            new(ClaimTypes.Name, appUser.Email ?? appUser.UserName ?? string.Empty),
            new("ApiKeyId", apiKey.PublicId)
        };

        // Add role claims
        var roles = await userManager.GetRolesAsync(appUser);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        // Fire-and-forget: update LastUsedAt
        var keyPublicId = apiKey.PublicId;
        _ = Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var key = await db.ApiKeys.FirstOrDefaultAsync(ak => ak.PublicId == keyPublicId);
            if (key != null)
            {
                key.LastUsedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
        });

        return AuthenticateResult.Success(ticket);
    }

    private static string HashKey(string plainTextKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainTextKey));
        return Convert.ToHexStringLower(bytes);
    }
}
