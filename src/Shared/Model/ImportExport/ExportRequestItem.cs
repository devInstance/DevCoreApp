using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Shared.Model.ImportExport;

public class ExportRequestItem
{
    public string EntityType { get; set; } = string.Empty;
    public ExportFileFormat Format { get; set; } = ExportFileFormat.Csv;
    public List<string> SelectedFields { get; set; } = new();
    public string Search { get; set; }
    public string[] SortBy { get; set; }
}
