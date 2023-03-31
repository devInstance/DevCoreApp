using DevInstance.DevCoreApp.Client.Services.Api;
using DevInstance.DevCoreApp.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using DevInstance.DevCoreApp.Client.Net.Api;
using DevInstance.DevCoreApp.Client.Net;
using DevInstance.DevCoreApp.Client.Net.ServicesMocks;

namespace DevInstance.DevCoreApp.Client.ClientMocks;

public static class ServiceExtensions
{
    public static IServiceCollection AddMoqAppServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IToolbarService, ToolbarService>();
        services.AddScoped<ISettingsService, SettingsService>();

        services.AddScoped<IWeatherForecastService, WeatherForecastServiceMock>();

        return services;
    }

    public static IServiceCollection AddMoqNetApi(this IServiceCollection service)
    {
        service.AddScoped<IAuthorizationApi, AuthorizationApi>();
        service.AddScoped<IUserProfileApi, UserProfileApi>();

        service.AddScoped<IWeatherForecastApi, WeatherForecastApi>();

        return service;
    }

}

