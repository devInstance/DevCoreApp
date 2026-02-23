using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models;

public enum AuditAction
{
    Insert = 0,
    Update = 1,
    Delete = 2
}

public enum AuditSource
{
    Application = 0,
    Database = 1
}

public class AuditLog : DatabaseBaseObject
{
    public Guid? OrganizationId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
    public AuditSource Source { get; set; }
}
