using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using System;
using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models;

public class Organization : DatabaseEntityObject
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public Organization? Parent { get; set; }
    public ICollection<Organization> Children { get; set; } = new List<Organization>();
    public int Level { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Settings { get; set; }
    public int SortOrder { get; set; }
}
