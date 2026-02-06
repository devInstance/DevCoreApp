using System.ComponentModel.DataAnnotations;

namespace DevInstance.DevCoreApp.Shared.Model.Account;

public class LoginParameters
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; }
}
