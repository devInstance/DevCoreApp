using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Services;
using DevInstance.DevCoreApp.Server.WebService.Indentity;
using DevInstance.DevCoreApp.Server.WebService.Tools;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.LogScope;
using System.Linq;
using System;

namespace DevInstance.DevCoreApp.Server.WebService.Services;

[AppService]
public class WeatherForecastService : BaseService
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly IScopeLog log;

    public WeatherForecastService(IScopeManager logManager, IQueryRepository query, IAuthorizationContext authorizationContext)
        : base(logManager, query, authorizationContext)
    {
        log = logManager.CreateLogger(this);
    }

    public ModelList<WeatherForecast> GetItems(int? top, int? page, int? filter, int? fields, string search)
    {
        using (log.TraceScope())
        {
            var rng = new Random();
            var items = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            }).ToArray();

            return new ModelList<WeatherForecast>
            {
                Count = items.Length,
                TotalCount = items.Length,
                Items = items,
                Page = 0,
                PagesCount = 1
            };
        }
    }
}
