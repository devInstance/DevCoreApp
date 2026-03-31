using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DevInstance.WebServiceToolkit.Common.Model;

namespace DevInstance.DevCoreApp.Shared.Model.ApiKeys;

public class ApiKeyItem : ModelItem
{
    [Required]
    [StringLength(256, MinimumLength = 2)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Prefix")]
    public string Prefix { get; set; } = string.Empty;

    [Display(Name = "Scopes")]
    public List<string>? Scopes { get; set; }

    [Display(Name = "Organization")]
    public string? OrganizationId { get; set; }

    public string? OrganizationName { get; set; }

    [Display(Name = "Expires At")]
    public DateTime? ExpiresAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? CreatedByName { get; set; }

    public DateTime CreateDate { get; set; }
}
