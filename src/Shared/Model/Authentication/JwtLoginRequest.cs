using System.ComponentModel.DataAnnotations;

namespace DevInstance.DevCoreApp.Shared.Model.Authentication;

public class JwtLoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
