using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Files;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Files;

public interface IFileService
{
    Task<ServiceActionResult<FileRecordItem>> UploadAsync(
        Stream stream, string originalName, string contentType,
        string? entityType = null, string? entityId = null);

    Task<ServiceActionResult<FileDownloadResult>> DownloadAsync(string filePublicId);

    Task<ServiceActionResult<bool>> DeleteAsync(string filePublicId);

    Task<ServiceActionResult<string>> GetUrlAsync(string filePublicId, TimeSpan? expiry = null);
}
