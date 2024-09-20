using DevInstance.DevCoreApp.Client.Net.Api;
using DevInstance.DevCoreApp.Client.Services.Api;
using DevInstance.DevCoreApp.Shared.Model;

namespace DevInstance.DevCoreApp.Client.Services;

public class WeatherForecastService : CRUDService<WeatherForecastItem>, IWeatherForecastService
{
    public WeatherForecastService(IWeatherForecastApi api) 
        : base(api)
    {
    }
}
