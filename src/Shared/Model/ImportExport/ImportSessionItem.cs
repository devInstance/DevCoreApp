using System;
using System.Collections.Generic;
using DevInstance.WebServiceToolkit.Common.Model;

namespace DevInstance.DevCoreApp.Shared.Model.ImportExport;

public class ImportSessionItem : ModelItem
{
    public string EntityType { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public ImportFileFormat FileFormat { get; set; }
    public ImportSessionStatus Status { get; set; }
    public string FileRecordId { get; set; }
    public List<ImportColumnMappingItem> ColumnMappings { get; set; }
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int ErrorRows { get; set; }
    public int ImportedRows { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
}
