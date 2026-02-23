using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models;

public class UserPermissionOverride : DatabaseBaseObject
{
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public Guid PermissionId { get; set; }
    public Permission? Permission { get; set; }

    public bool IsGranted { get; set; }
    public string? Reason { get; set; }
}
