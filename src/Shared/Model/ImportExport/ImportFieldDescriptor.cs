namespace DevInstance.DevCoreApp.Shared.Model.ImportExport;

public class ImportFieldDescriptor
{
    public string Field { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string DataType { get; set; } = "string";
    public string? Description { get; set; }
}
