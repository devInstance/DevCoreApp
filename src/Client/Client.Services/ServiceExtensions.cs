using DevInstance.DevCoreApp.Client.Services.Api;
using DevInstance.DevCoreApp.Client.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;

namespace DevInstance.DevCoreApp.Client.Services;

public static class ServiceExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddNetApi();

        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IToolbarService, ToolbarService>();
        services.AddScoped<ISettingsService, SettingsService>();

        return services;
    }

}

