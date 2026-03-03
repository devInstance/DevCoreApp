namespace DevInstance.DevCoreApp.Shared.Model.ImportExport;

public class ImportColumnMappingItem
{
    public int SourceColumnIndex { get; set; }
    public string SourceColumnName { get; set; } = string.Empty;
    public string? TargetField { get; set; }
}
