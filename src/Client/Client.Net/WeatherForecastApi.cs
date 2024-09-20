using DevInstance.DevCoreApp.Client.Net.Api;
using DevInstance.DevCoreApp.Shared.Model;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Client.Net
{
    public class WeatherForecastApi : CRUDApi<WeatherForecastItem>, IWeatherForecastApi
    {
        public WeatherForecastApi(HttpClient http, NavigationManager navigationManager) : base(http, navigationManager, "api/forecast")
        {
        }
    }
}
