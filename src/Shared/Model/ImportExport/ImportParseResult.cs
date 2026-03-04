using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Shared.Model.ImportExport;

public class ImportParseResult
{
    public List<string> Headers { get; set; } = new();
    public int RowCount { get; set; }
    public ImportFileFormat Format { get; set; }
}
