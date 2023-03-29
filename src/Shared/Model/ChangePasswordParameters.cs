using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DevInstance.DevCoreApp.Shared.Model;

public class ChangePasswordParameters
{
    [Required]
    public string OldPassword { get; set; }
    [Required]
    public string NewPassword { get; set; }
    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public string NewPasswordConfirm { get; set; }
}
