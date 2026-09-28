namespace DevInstance.DevCoreApp.Client.Services.Core.Auth;

/// <summary>The signed-in session: a short-lived access token and the rotating refresh token.</summary>
public sealed record AuthTokens(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);
