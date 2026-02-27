using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Shared.Model.Roles;

public class RolePermissionsRequest
{
    public List<string> PermissionKeys { get; set; } = new();
}
