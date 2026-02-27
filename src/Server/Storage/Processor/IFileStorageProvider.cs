using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.StorageProcessor
{
    public interface IFileStorageProvider
    {
        Task<FileUploadResult> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default);
        Task<Stream> DownloadAsync(string storagePath, CancellationToken ct = default);
        Task DeleteAsync(string storagePath, CancellationToken ct = default);
        Task<string> GetUrlAsync(string storagePath, TimeSpan? expiry = null, CancellationToken ct = default);
        Task<bool> ExistsAsync(string storagePath, CancellationToken ct = default);
    }
}
