using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models;

/// <summary>
/// Structured application log entry persisted to the database.
/// Written by the Serilog PostgreSQL sink — not by application code directly.
/// </summary>
public class ApplicationLog : DatabaseBaseObject
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }

    /// <summary>
    /// JSON-serialized structured log properties.
    /// </summary>
    public string? Properties { get; set; }

    public string? CorrelationId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? OrganizationId { get; set; }
}
