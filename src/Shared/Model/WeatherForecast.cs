using System;
using System.Collections.Generic;
using System.Text;

namespace DevInstance.DevCoreApp.Shared.Model;

public class WeatherForecast : EntityItem
{
    public DateTime Date { get; set; }

    public int TemperatureC { get; set; }

    public string Summary { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
