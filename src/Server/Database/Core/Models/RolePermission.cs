using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models;

public class RolePermission
{
    public Guid RoleId { get; set; }
    public ApplicationRole? Role { get; set; }

    public Guid PermissionId { get; set; }
    public Permission? Permission { get; set; }
}
