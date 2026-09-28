namespace DevInstance.DevCoreApp.Client.Services.Core.ImportExport;

/// <summary>A file produced by the server (export, import template), downloaded into memory.</summary>
public class ExportDownloadResult
{
    public Stream Stream { get; set; } = Stream.Null;
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
