using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Shared.Model.ImportExport;

public class ImportValidationResult
{
    public string SessionId { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int WarningRows { get; set; }
    public int ErrorRows { get; set; }
    public List<ImportRowPreviewItem> Rows { get; set; } = new();
}
