using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models;

/// <summary>
/// Application setting with four-tier scoping.
///
/// Scope resolution (all nullable FKs determine the tier):
///   - All null              → System-level setting (global default)
///   - TenantId only         → Tenant-level override
///   - OrganizationId set    → Organization-level override
///   - UserId set            → User-level override (most specific)
///
/// Resolution order: User → Organization → Tenant → System (first non-null wins).
/// </summary>
public class Setting : DatabaseBaseObject
{
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized value. Stored as jsonb in PostgreSQL.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Describes the CLR type for deserialization: "string", "int", "bool", "json".
    /// </summary>
    public string ValueType { get; set; } = "string";

    public string? Description { get; set; }

    /// <summary>
    /// When true, the Value should be excluded from audit logs and masked in API responses.
    /// </summary>
    public bool IsSensitive { get; set; }
}
