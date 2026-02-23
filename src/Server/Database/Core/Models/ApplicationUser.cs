using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models;

public enum AccountStatus
{
    Active = 0,
    Inactive = 1,
    Locked = 2,
    PendingApproval = 3
}

public class ApplicationUser : IdentityUser<Guid>
{
    public AccountStatus Status { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public Guid? PrimaryOrganizationId { get; set; }
    public Organization? PrimaryOrganization { get; set; }

    [AuditExclude]
    public override string? PasswordHash { get => base.PasswordHash; set => base.PasswordHash = value; }

    [AuditExclude]
    public override string? SecurityStamp { get => base.SecurityStamp; set => base.SecurityStamp = value; }

    public List<UserOrganization> UserOrganizations { get; set; } = new();
}
