namespace DevInstance.DevCoreApp.Server.Admin.Services.ImportExport;

public class ExportDownloadResult
{
    public Stream Stream { get; set; } = Stream.Null;
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
