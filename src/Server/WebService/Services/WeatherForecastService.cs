using DevInstance.BlazorToolkit.Model;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Services.Server;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Exceptions;
using DevInstance.DevCoreApp.Server.WebService.Authentication;
using DevInstance.DevCoreApp.Server.WebService.Tools;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.DevCoreApp.Shared.Services;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using Microsoft.EntityFrameworkCore;

namespace DevInstance.DevCoreApp.Server.WebService.Services;

[AppService]
public class WeatherForecastService : BaseService, IWeatherForecastService
{
    public WeatherForecastService(IScopeManager logManager, ITimeProvider timeProvider, IQueryRepository query, IAuthorizationContext authorizationContext)
        : base(logManager, timeProvider, query, authorizationContext)
    {
        log = logManager.CreateLogger(this);
    }

    public event DataUpdate<WeatherForecastItem> OnDataUpdate;

    public async Task<ModelList<WeatherForecastItem>?> GetItemsAsync(int? top, int? page, string? sortBy, bool? isAsc, int? filter, int? fields, string? search)
    {
        using (log.TraceScope())
        {
            var coreQuery = Repository.GetWeatherForecastQuery(AuthorizationContext.CurrentProfile);
            var result = CreateList<WeatherForecastItem>();

            coreQuery = ApplyFilters(result, coreQuery, filter, search);

            var pagedQuery = coreQuery.Clone();
            pagedQuery = ApplyPages(pagedQuery, top, page);
            pagedQuery = ApplySorting(result, pagedQuery, sortBy, isAsc);

            var list = await pagedQuery.Select().ToViewAsync();

            return ApplyItems(result, await coreQuery.Select().CountAsync(), list.ToArray(), top, page);
        }
    }

    public async Task<ServiceActionResult<ModelList<WeatherForecastItem>?>> GetItemsAsync(int? top, int? page, WeatherForecastFields? sortBy, bool? isAsc, string? search)
    {
        using (log.TraceScope())
        {
            return await ServiceUtils.HandleServiceCallAsync(log,
                async (l) =>
                {
                    return await GetItemsAsync(top, page, sortBy?.ToFieldName(), isAsc, null, null, search);
                }
            );
        }
    }

    private static void Validate(WeatherForecastItem item)
    {
        if (item.TemperatureC < -273 || String.IsNullOrWhiteSpace(item.Summary))
        {
            throw new BadRequestException();
        }
    }

    public async Task<WeatherForecastItem> AddAsync(WeatherForecastItem item)
    {
        Validate(item);

        var q = Repository.GetWeatherForecastQuery(AuthorizationContext.CurrentProfile);

        var record = q.CreateNew().ToRecord(item);

        await q.AddAsync(record);

        return await GetByIdAsync(record.PublicId);
    }


    public async Task<WeatherForecastItem> GetByIdAsync(string id)
    {
        return (await GetRecordByPublicIdAsync(id)).ToView();
    }

    private async Task<WeatherForecast> GetRecordByPublicIdAsync(string id)
    {
        var q = Repository.GetWeatherForecastQuery(AuthorizationContext.CurrentProfile).ByPublicId(id);
        var record = await q.Select().FirstOrDefaultAsync();
        if (record == null)
        {
            throw new RecordNotFoundException();
        }

        return record;
    }

    public async Task<WeatherForecastItem> RemoveAsync(string id)
    {
        var q = Repository.GetWeatherForecastQuery(AuthorizationContext.CurrentProfile);
        var record = await GetRecordByPublicIdAsync(id);

        await q.RemoveAsync(record);

        return record.ToView();
    }

    public async Task<WeatherForecastItem> UpdateAsync(string id, WeatherForecastItem item)
    {
        Validate(item);

        var q = Repository.GetWeatherForecastQuery(AuthorizationContext.CurrentProfile);
        var record = await GetRecordByPublicIdAsync(id);

        await q.UpdateAsync(record.ToRecord(item));

        return await GetByIdAsync(record.PublicId);
    }

    public Task<BlazorToolkit.Services.ServiceActionResult<WeatherForecastItem?>> GetAsync(string id)
    {
        throw new NotImplementedException();
    }

    public Task<BlazorToolkit.Services.ServiceActionResult<WeatherForecastItem?>> AddNewAsync(WeatherForecastItem item)
    {
        throw new NotImplementedException();
    }

    public Task<BlazorToolkit.Services.ServiceActionResult<WeatherForecastItem?>> UpdateAsync(WeatherForecastItem item)
    {
        throw new NotImplementedException();
    }

    public Task<BlazorToolkit.Services.ServiceActionResult<bool>> RemoveAsync(WeatherForecastItem item)
    {
        throw new NotImplementedException();
    }
}
