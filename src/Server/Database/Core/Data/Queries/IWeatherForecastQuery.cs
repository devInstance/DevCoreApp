using DevInstance.DevCoreApp.Server.Database.Core.Models;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IWeatherForecastQuery : IModelQuery<WeatherForecast, IWeatherForecastQuery>, 
                                        IQSearchable<IWeatherForecastQuery>,
                                        IQPageable<IWeatherForecastQuery>
{
    IQueryable<WeatherForecast> Select();
}
