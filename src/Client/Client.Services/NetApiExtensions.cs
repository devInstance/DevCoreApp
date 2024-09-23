using DevInstance.DevCoreApp.Client.Net.Api;
using Microsoft.Extensions.DependencyInjection;

namespace DevInstance.DevCoreApp.Client.Net;

public static class NetApiExtensions
{
    public static IServiceCollection AddNetApi(this IServiceCollection service)
    {
        service.AddScoped<INetApiRepository, NetApiRepository>();

        return service;
    }
}

