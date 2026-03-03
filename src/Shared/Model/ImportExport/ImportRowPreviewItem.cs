using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Shared.Model.ImportExport;

public class ImportRowPreviewItem
{
    public int RowNumber { get; set; }
    public ImportRowStatus Status { get; set; }
    public Dictionary<string, string> Values { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
