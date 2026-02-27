using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.StorageProcessor.S3
{
    public class S3FileStorageProvider : IFileStorageProvider
    {
        private readonly StorageConfiguration _config;

        public S3FileStorageProvider(StorageConfiguration config)
        {
            _config = config;
        }

        public Task<FileUploadResult> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
        {
            throw new NotImplementedException("S3 storage provider is not yet implemented.");
        }

        public Task<Stream> DownloadAsync(string storagePath, CancellationToken ct = default)
        {
            throw new NotImplementedException("S3 storage provider is not yet implemented.");
        }

        public Task DeleteAsync(string storagePath, CancellationToken ct = default)
        {
            throw new NotImplementedException("S3 storage provider is not yet implemented.");
        }

        public Task<string> GetUrlAsync(string storagePath, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            throw new NotImplementedException("S3 storage provider is not yet implemented.");
        }

        public Task<bool> ExistsAsync(string storagePath, CancellationToken ct = default)
        {
            throw new NotImplementedException("S3 storage provider is not yet implemented.");
        }
    }
}
