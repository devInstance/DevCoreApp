using System.IO;

namespace DevInstance.DevCoreApp.Server.Services.Core.Files;

public class FileDownloadResult
{
    public Stream Stream { get; set; } = null!;
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
