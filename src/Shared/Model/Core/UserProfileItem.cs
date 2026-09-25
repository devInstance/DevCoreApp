using DevInstance.WebServiceToolkit.Common.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DevInstance.DevCoreApp.Shared.Model.Core;

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

    /// <summary>The machine value — the <c>UserStatus</c> enum name. Compare against this, display <see cref="StatusLabel"/>.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>The human-readable form of <see cref="Status"/>. See <c>UserStatusLabels</c>.</summary>
    public string StatusLabel => UserAdmin.UserStatusLabels.For(Status);

    /// <summary>
    /// The name of the user's primary organization.
    /// <para>
    /// Populated only by the list read (<c>GetListAsync</c>), which batches the lookup for the whole
    /// page; single-user reads leave it empty rather than pay for a join nothing displays. Empty
    /// also legitimately means "no assignment" — worth noticing, since such a user reads every
    /// organization and can write to none.
    /// </para>
    /// </summary>
    [Display(Name = "Organization")]
    public string OrganizationName { get; set; } = string.Empty;

    [Display(Name = "Time Zone")]
    public string? TimeZoneId { get; set; }

    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }

    public string? ProfilePictureUrl { get; set; }
    public string? ProfilePictureThumbnailUrl { get; set; }
    public bool HasProfilePicture { get; set; }

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
