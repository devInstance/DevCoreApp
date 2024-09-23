using DevInstance.DevCoreApp.Client.Net.Api;
using DevInstance.DevCoreApp.Shared.Model;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Client.Net;

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
