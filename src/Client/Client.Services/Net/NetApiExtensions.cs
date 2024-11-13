using DevInstance.DevCoreApp.Client.Services.Net.Api;
using Microsoft.Extensions.DependencyInjection;

namespace DevInstance.DevCoreApp.Client.Services.Net;

public static class NetApiExtensions
{
    public static IServiceCollection AddNetApi(this IServiceCollection service)
    {
        service.AddScoped<INetApiRepository, NetApiRepository>();

        return service;
    }
}

