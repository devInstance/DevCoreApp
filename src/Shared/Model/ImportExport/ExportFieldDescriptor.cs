namespace DevInstance.DevCoreApp.Shared.Model.ImportExport;

public class ExportFieldDescriptor
{
    public string Field { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
