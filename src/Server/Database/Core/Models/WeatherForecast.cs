using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models;

public class WeatherForecast : DatabaseEntityObject
{
    public DateTime Date { get; set; }

    public int Temperature { get; set; }

    public string Summary { get; set; }
}
