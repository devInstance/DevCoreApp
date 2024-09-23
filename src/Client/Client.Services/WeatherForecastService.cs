using DevInstance.DevCoreApp.Client.Net;
using DevInstance.DevCoreApp.Client.Net.Api;
using DevInstance.DevCoreApp.Client.Services.Api;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.BlazorUtils.Services;

namespace DevInstance.DevCoreApp.Client.Services;

public class WeatherForecastService : CRUDService<WeatherForecastItem>, IWeatherForecastService
{
    private INetApiRepository Api { get; }

    public WeatherForecastService(INetApiRepository api) 
        : base(api.GetWeatherForecastApi())
    {
        Api = api;
    }

    public async Task<ServiceActionResult<ModelList<WeatherForecastItem>?>> GetItemsAsync(int? top, int? page, WeatherForecastFields? sortBy, bool? isAsc, string? search)
    {
        return await HandleWebApiCallAsync(
            async () =>
            {
                var api = Api.GetWeatherForecastApi().Get();
                if(top.HasValue)
                {
                    api = api.Top(top.Value);
                }
                if (page.HasValue)
                {
                    api = api.Page(page.Value);
                }
                if (sortBy.HasValue)
                {
                    api = api.Sort(sortBy.Value.ToString(), isAsc ?? true);
                }

                return await api.ListAsync();
            }
        );
    }
}
