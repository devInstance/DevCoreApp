using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models;

public class Permission : DatabaseBaseObject
{
    public string Module { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserPermissionOverride> UserPermissionOverrides { get; set; } = new List<UserPermissionOverride>();
}
