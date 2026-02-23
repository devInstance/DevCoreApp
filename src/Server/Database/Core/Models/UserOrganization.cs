using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using DevInstance.DevCoreApp.Shared.Model.Common;
using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models;

public class UserOrganization : DatabaseBaseObject
{
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public OrganizationAccessScope Scope { get; set; }
    public bool IsPrimary { get; set; }
}
