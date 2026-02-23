using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models;

public enum TenantStatus
{
    Active = 0,
    Suspended = 1,
    Deactivated = 2
}

public class Tenant : DatabaseEntityObject
{
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public TenantStatus Status { get; set; }
    public string Plan { get; set; } = string.Empty;
    public string? Settings { get; set; }
    public Guid? RootOrganizationId { get; set; }
    public Organization? RootOrganization { get; set; }
}
