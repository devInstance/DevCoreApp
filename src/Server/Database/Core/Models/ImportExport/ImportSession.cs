using System;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using DevInstance.DevCoreApp.Shared.Model.ImportExport;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models.ImportExport;

public class ImportSession : DatabaseObject, IOrganizationScoped
{
    public Guid OrganizationId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public ImportFileFormat FileFormat { get; set; }
    public ImportSessionStatus Status { get; set; }
    public string? FileRecordId { get; set; }
    public string? ColumnMappingJson { get; set; }
    public string? ValidationResultJson { get; set; }
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int ErrorRows { get; set; }
    public int ImportedRows { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? CreatedById { get; set; }

    public UserProfile? CreatedBy { get; set; }
    public Organization? Organization { get; set; }
}
