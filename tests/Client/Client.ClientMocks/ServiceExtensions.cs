using DevInstance.DevCoreApp.Client.Services.Api;
using Microsoft.Extensions.DependencyInjection;
using DevInstance.DevCoreApp.Client.Net.Api;
using DevInstance.DevCoreApp.Client.Net;
using DevInstance.DevCoreApp.Client.Net.ServicesMocks;
using DevInstance.DevCoreApp.Client.ClientMocks.ServicesMocks;

namespace DevInstance.DevCoreApp.Client.Services;

public static class ServiceExtensions
{
    public static IServiceCollection AddMoqAppServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationService, AuthorizationServiceMock>();

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

