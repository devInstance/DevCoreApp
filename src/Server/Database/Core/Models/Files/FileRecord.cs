using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models.Files;

public class FileRecord : DatabaseEntityObject, IOrganizationScoped
{
    public Guid OrganizationId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StorageProvider { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }

    public Organization Organization { get; set; } = null!;
}
