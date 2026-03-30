using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DevInstance.WebServiceToolkit.Common.Model;

namespace DevInstance.DevCoreApp.Shared.Model.FeatureFlags;

public class FeatureFlagItem : ModelItem
{
    [Required]
    [StringLength(256, MinimumLength = 2)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; }

    public string? OrganizationId { get; set; }

    public string? OrganizationName { get; set; }

    [Range(0, 100)]
    [Display(Name = "Rollout Percentage")]
    public int? RolloutPercentage { get; set; }

    [Display(Name = "Allowed Users")]
    public List<string>? AllowedUsers { get; set; }

    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
}
