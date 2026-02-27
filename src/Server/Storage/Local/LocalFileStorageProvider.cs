using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.StorageProcessor.Local
{
    public class LocalFileStorageProvider : IFileStorageProvider
    {
        private readonly StorageConfiguration _config;

        public LocalFileStorageProvider(StorageConfiguration config)
        {
            _config = config;

            if (!Directory.Exists(_config.BasePath))
            {
                Directory.CreateDirectory(_config.BasePath);
            }
        }

        public async Task<FileUploadResult> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
        {
            try
            {
                var storagePath = Path.Combine(
                    DateTime.UtcNow.ToString("yyyy/MM/dd"),
                    $"{Guid.NewGuid():N}_{fileName}");

                var fullPath = Path.Combine(_config.BasePath, storagePath);
                var directory = Path.GetDirectoryName(fullPath)!;

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
                await stream.CopyToAsync(fileStream, ct);

                return new FileUploadResult
                {
                    Success = true,
                    StoragePath = storagePath,
                    SizeBytes = fileStream.Length
                };
            }
            catch (Exception ex)
            {
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public Task<Stream> DownloadAsync(string storagePath, CancellationToken ct = default)
        {
            var fullPath = Path.Combine(_config.BasePath, storagePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {storagePath}");
            }

            Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            return Task.FromResult(stream);
        }

        public Task DeleteAsync(string storagePath, CancellationToken ct = default)
        {
            var fullPath = Path.Combine(_config.BasePath, storagePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }

        public Task<string> GetUrlAsync(string storagePath, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_config.BaseUrl))
            {
                return Task.FromResult(storagePath);
            }

            var url = $"{_config.BaseUrl.TrimEnd('/')}/{storagePath.Replace('\\', '/')}";
            return Task.FromResult(url);
        }

        public Task<bool> ExistsAsync(string storagePath, CancellationToken ct = default)
        {
            var fullPath = Path.Combine(_config.BasePath, storagePath);
            return Task.FromResult(File.Exists(fullPath));
        }
    }
}
