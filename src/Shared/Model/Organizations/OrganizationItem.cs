using DevInstance.WebServiceToolkit.Common.Model;
using System;
using System.ComponentModel.DataAnnotations;

namespace DevInstance.DevCoreApp.Shared.Model.Organizations;

public class OrganizationItem : ModelItem
{
    [Required]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Code")]
    public string Code { get; set; } = string.Empty;

    public string? ParentId { get; set; }

    public int Level { get; set; }

    public string Path { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Type")]
    public string Type { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public string? Settings { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime UpdateDate { get; set; }
}
