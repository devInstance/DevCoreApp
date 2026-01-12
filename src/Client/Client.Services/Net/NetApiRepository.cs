using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.BlazorToolkit.Http;
using Microsoft.AspNetCore.Components;
using DevInstance.DevCoreApp.Client.Services.Net.Api;
using System.Net.Http;

namespace DevInstance.DevCoreApp.Client.Services.Net;

public class NetApiRepository : INetApiRepository
{
    private readonly IHttpClientFactory clientFactory;
    private readonly IHttpApiContextFactory apiFactory;

    public NetApiRepository(IHttpClientFactory client, IHttpApiContextFactory api,NavigationManager navigationManager)
    {
        clientFactory = client;
        apiFactory = api;
    }

    HttpClient HttpClient => clientFactory.CreateClient("DevInstance.DevCoreApp.ServerAPI");
    public IApiContext<WeatherForecastItem> GetWeatherForecastApi()
    {
        return apiFactory.Create<WeatherForecastItem>(HttpClient, "api/forecast");
    }
}
