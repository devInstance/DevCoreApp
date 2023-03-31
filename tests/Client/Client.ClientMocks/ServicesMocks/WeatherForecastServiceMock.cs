using DevInstance.DevCoreApp.Client.Services;
using DevInstance.DevCoreApp.Client.Services.Api;
using DevInstance.DevCoreApp.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Client.Net.ServicesMocks;

internal class WeatherForecastServiceMock : IWeatherForecastService
{
    public event DataUpdate<WeatherForecastItem> OnDataUpdate;

    public Task<ServiceActionResult<WeatherForecastItem?>> AddNewAsync(WeatherForecastItem item)
    {
        throw new NotImplementedException();
    }

    public Task<ServiceActionResult<WeatherForecastItem?>> GetAsync(string id)
    {
        throw new NotImplementedException();
    }

    public Task<ServiceActionResult<ModelList<WeatherForecastItem>?>> GetItemsAsync(int? top, int? page, ItemFilters? filters, ItemFields? fields, string? search)
    {
        throw new NotImplementedException();
    }

    public Task<ServiceActionResult<bool>> RemoveAsync(WeatherForecastItem item)
    {
        throw new NotImplementedException();
    }

    public Task<ServiceActionResult<WeatherForecastItem?>> UpdateAsync(WeatherForecastItem item)
    {
        throw new NotImplementedException();
    }
}
