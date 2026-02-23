using System;

namespace DevInstance.DevCoreApp.Shared.Model.Authentication;

public class JwtLoginResponse
{
    public bool Succeeded { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }

    public static JwtLoginResponse Success(string accessToken, string refreshToken, DateTime expiresAt) => new()
    {
        Succeeded = true,
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        ExpiresAt = expiresAt
    };

    public static JwtLoginResponse Failure(string errorMessage) => new()
    {
        Succeeded = false,
        ErrorMessage = errorMessage
    };
}
