using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.BlazorToolkit.Services;
using DevInstance.LogScope;
using DevInstance.DevCoreApp.Client.Services.Net.Api;
using DevInstance.DevCoreApp.Shared.Services;
using DevInstance.BlazorToolkit.Services.Wasm;

namespace DevInstance.DevCoreApp.Client.Services;

public class WeatherForecastService : CRUDService<WeatherForecastItem>, IWeatherForecastService
{
    private INetApiRepository Api { get; }

    public WeatherForecastService(INetApiRepository api, IScopeManager lp) 
        : base(api.GetWeatherForecastApi())
    {
        Log = lp.CreateLogger(this);
        Api = api;
    }

    public async Task<ServiceActionResult<ModelList<WeatherForecastItem>?>> GetItemsAsync(int? top, int? page, WeatherForecastFields? sortBy, bool? isAsc, string? search)
    {
        using (var log = Log.TraceScope())
        {
            return await ServiceUtils.HandleWebApiCallAsync(log,
                async (l) =>
                {
                    var api = Api.GetWeatherForecastApi().Get();
                    if (top.HasValue)
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
}
