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
    private static readonly string[] Summaries = new[]
    {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public async Task<WeatherForecastItem[]> GetForecastAsync()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecastItem
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToArray();
    }

    public event DataUpdate<WeatherForecastItem> OnDataUpdate;

    public Task<ServiceActionResult<WeatherForecastItem?>> AddNewAsync(WeatherForecastItem item)
    {
        throw new NotImplementedException();
    }

    public Task<ServiceActionResult<WeatherForecastItem?>> GetAsync(string id)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceActionResult<ModelList<WeatherForecastItem>?>> GetItemsAsync(int? top, int? page, ItemFilters? filters, ItemFields? fields, string? search)
    {
        return new ServiceActionResult<ModelList<WeatherForecastItem>?> { Success = true, Result = new ModelList<WeatherForecastItem> { Items = await GetForecastAsync() } };
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
