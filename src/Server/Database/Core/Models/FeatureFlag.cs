using System;
using System.Collections.Generic;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models;

public class FeatureFlag : DatabaseObject
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
    public Guid? OrganizationId { get; set; }
    public int? RolloutPercentage { get; set; }
    public List<string>? AllowedUsers { get; set; }

    public Organization? Organization { get; set; }
}
