using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DevInstance.DevCoreApp.Shared.Model;

public class ForgotPasswordParameters
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}
