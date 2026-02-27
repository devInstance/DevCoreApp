using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevInstance.DevCoreApp.Server.StorageProcessor.S3
{
    public static class ConfigurationExtensions
    {
        public static void AddS3FileStorage(this IServiceCollection services, IConfiguration configuration)
        {
            var storageConfig = new StorageConfiguration
            {
                Provider = "S3",
                BasePath = configuration["StorageConfiguration:BucketName"] ?? string.Empty,
                BaseUrl = configuration["StorageConfiguration:BaseUrl"]
            };
            services.AddScoped<IFileStorageProvider>(x => new S3FileStorageProvider(storageConfig));
        }
    }
}
