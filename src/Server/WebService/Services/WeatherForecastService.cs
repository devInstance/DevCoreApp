using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Exceptions;
using DevInstance.DevCoreApp.Server.Services;
using DevInstance.DevCoreApp.Server.WebService.Indentity;
using DevInstance.DevCoreApp.Server.WebService.Tools;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.WebService.Services;

[AppService]
public class WeatherForecastService : BaseService
{
    private readonly IScopeLog log;

    public WeatherForecastService(IScopeManager logManager, ITimeProvider timeProvider, IQueryRepository query, IAuthorizationContext authorizationContext)
        : base(logManager, timeProvider, query, authorizationContext)
    {
        log = logManager.CreateLogger(this);
    }

    public ModelList<WeatherForecastItem> GetItems(int? top, int? page, int? filter, int? fields, string search)
    {
        using (log.TraceScope())
        {
            var coreQuery = Repository.GetWeatherForecastQuery(AuthorizationContext.CurrentProfile);

            coreQuery = ApplyFilters(coreQuery, filter, search);

            var pagedQuery = coreQuery.Clone();
            pagedQuery = ApplyPages(pagedQuery, top, page);

            var list = pagedQuery.Select().ToView();

            return CreateListPage(coreQuery.Select().Count(), list.ToArray(), top, page);
        }
    }

    private static void Validate(WeatherForecastItem item)
    {
        if (item.TemperatureC < -273 || String.IsNullOrWhiteSpace(item.Summary))
        {
            throw new BadRequestException();
        }
    }

    public WeatherForecastItem Add(WeatherForecastItem item)
    {
        Validate(item);
        
        DateTime now = TimeProvider.CurrentTime;

        var q = Repository.GetWeatherForecastQuery(AuthorizationContext.CurrentProfile);

        var record = q.CreateNew().ToRecord(item);

        q.Add(record);

        return GetById(record.PublicId);
    }


    public WeatherForecastItem GetById(string id)
    {
        var record = GetRecordByPublicId(id);
        return record.ToView();
    }

    private WeatherForecast GetRecordByPublicId(string id)
    {
        var q = Repository.GetWeatherForecastQuery(AuthorizationContext.CurrentProfile).ByPublicId(id);
        var record = q.Select().FirstOrDefault();
        if (record == null)
        {
            throw new RecordNotFoundException();
        }

        return record;
    }

    public WeatherForecastItem Remove(string id)
    {
        var q = Repository.GetWeatherForecastQuery(AuthorizationContext.CurrentProfile);
        var record = GetRecordByPublicId(id);
        
        q.Remove(record);
        
        return record.ToView();
    }

    public WeatherForecastItem Update(string id, WeatherForecastItem item)
    {
        Validate(item);

        var q = Repository.GetWeatherForecastQuery(AuthorizationContext.CurrentProfile);
        var record = GetRecordByPublicId(id);

        q.Update(record.ToRecord(item));

        return GetById(record.PublicId);
    }
}
