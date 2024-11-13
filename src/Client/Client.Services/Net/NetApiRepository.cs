using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.BlazorUtils.Http;
using Microsoft.AspNetCore.Components;
using DevInstance.DevCoreApp.Client.Services.Net.Api;

namespace DevInstance.DevCoreApp.Client.Services.Net;

public class NetApiRepository : INetApiRepository
{
    private HttpClient httpClient;

    public NetApiRepository(HttpClient http, NavigationManager navigationManager)
    {
        httpClient = http;
        httpClient.BaseAddress = new Uri(navigationManager.BaseUri);
    }

    public IApiContext<WeatherForecastItem> GetWeatherForecastApi()
    {
        return HttpApiContextFactory.Create<WeatherForecastItem>(httpClient, "api/forecast");
    }
}
