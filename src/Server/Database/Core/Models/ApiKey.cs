using System;
using System.Collections.Generic;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models;

public class ApiKey : DatabaseObject
{
    public string Name { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public List<string>? Scopes { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid CreatedById { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }

    public UserProfile CreatedBy { get; set; } = default!;
    public Organization? Organization { get; set; }
}
