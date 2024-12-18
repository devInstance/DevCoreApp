using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.WebServiceToolkit.Database.Queries;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IWeatherForecastQuery : IModelQuery<WeatherForecast, IWeatherForecastQuery>, 
                                        IQSearchable<IWeatherForecastQuery>,
                                        IQPageable<IWeatherForecastQuery>,
                                        IQSortable<IWeatherForecastQuery>
{
    IQueryable<WeatherForecast> Select();
}
