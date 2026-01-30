using DevInstance.WebServiceToolkit.Common.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DevInstance.DevCoreApp.Shared.Model;

public class UserProfileItem : ModelItem
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; }

    [Display(Name = "Middle Name")]
    public string MiddleName { get; set; }

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; }

    [Phone]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; }

    public string Roles { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }

    public string FullName
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(FirstName)) parts.Add(FirstName);
            if (!string.IsNullOrWhiteSpace(MiddleName)) parts.Add(MiddleName);
            if (!string.IsNullOrWhiteSpace(LastName)) parts.Add(LastName);
            return string.Join(" ", parts);
        }
    }
}
