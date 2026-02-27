using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevInstance.DevCoreApp.Server.StorageProcessor.Local
{
    public static class ConfigurationExtensions
    {
        public static void AddLocalFileStorage(this IServiceCollection services, IConfiguration configuration)
        {
            var storageConfig = new StorageConfiguration
            {
                Provider = "Local",
                BasePath = configuration["StorageConfiguration:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "storage"),
                BaseUrl = configuration["StorageConfiguration:BaseUrl"]
            };
            services.AddScoped<IFileStorageProvider>(x => new LocalFileStorageProvider(storageConfig));
        }
    }
}
