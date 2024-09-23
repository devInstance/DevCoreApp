using DevInstance.DevCoreApp.Shared.Model;

namespace DevInstance.DevCoreApp.Client.Services.Api;

public enum WeatherForecastFields
{
    Date,
    Temperature,
    Summary
}

public interface IWeatherForecastService : ICRUDService<WeatherForecastItem>
{
    Task<ServiceActionResult<ModelList<WeatherForecastItem>?>> GetItemsAsync(int? top, int? page, WeatherForecastFields? sortBy, bool? isAsc, string? search);

}
