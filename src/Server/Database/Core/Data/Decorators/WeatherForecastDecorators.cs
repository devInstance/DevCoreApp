using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Model;
using System.Collections.Generic;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;

public static class WeatherForecastDecorators
{
    public static WeatherForecastItem ToView(this WeatherForecast record)
    {
        if (record == null) return null;

        return new WeatherForecastItem
        {
            Id = record.PublicId,
            Summary = record.Summary,
            Date = record.Date,
            TemperatureC = record.Temperature,
            CreateDate = record.CreateDate,
            CreatedBy = record.CreatedBy.ToView(),
            UpdateDate = record.UpdateDate,
            UpdatedBy = record.UpdatedBy.ToView(),
        };
    }

    public static List<WeatherForecastItem> ToView(this IQueryable<WeatherForecast> query)
    {
        return (from inv in query select inv.ToView()).ToList();
    }

    public static WeatherForecast ToRecord(this WeatherForecast record, WeatherForecastItem item)
    {
        if (record == null) return null;

        record.Temperature = item.TemperatureC;
        record.Date = item.Date;
        record.Summary = item.Summary;

        return record;
    }

}