using DevInstance.WebServiceToolkit.Common.Model;
using System.ComponentModel.DataAnnotations;

namespace DevInstance.DevCoreApp.Shared.Model.Roles;

public class RoleItem : ModelItem
{
    [Required]
    [StringLength(256, MinimumLength = 2)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    public bool IsSystemRole { get; set; }

    public int PermissionCount { get; set; }
}
