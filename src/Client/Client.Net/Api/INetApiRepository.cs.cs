using DevInstance.DevCoreApp.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Client.Net.Api
{
    public interface INetApiRepository
    {
        IApiContext<WeatherForecastItem> GetWeatherForecastApi();
    }
}
