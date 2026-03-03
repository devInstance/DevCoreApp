using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Shared.Model.ImportExport;

public class ImportCommitResult
{
    public string SessionId { get; set; } = string.Empty;
    public int ImportedRows { get; set; }
    public int SkippedRows { get; set; }
    public int ErrorRows { get; set; }
    public List<string> Errors { get; set; } = new();
}
