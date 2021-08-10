using Microsoft.Extensions.DependencyInjection;

namespace DevInstance.SampleWebApp.Server.Services
{
    public static class ServiceConfigurationExtensions
    {
        public static void ConfigureServices(this IServiceCollection services)
        {
            services.AddScoped<AuthorizationService>();
            services.AddScoped<UserProfileService>();
        }
    }
}
