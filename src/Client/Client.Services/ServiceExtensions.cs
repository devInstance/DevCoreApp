using DevInstance.DevCoreApp.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevInstance.DevCoreApp.Client.Services;

public static class ServiceExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
//        services.AddScoped<IToolbarService, ToolbarService>();
//        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IWeatherForecastService, WeatherForecastService>();

        return services;
    }

}

