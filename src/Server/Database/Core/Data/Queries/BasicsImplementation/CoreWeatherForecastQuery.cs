using DevInstance.LogScope;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NoCrast.Server.Database.Postgres.Data.Queries;

public class CoreWeatherForecastQuery : CoreBaseQuery, IWeatherForecastQuery
{
    private IQueryable<WeatherForecast> currentQuery;

    private string sortedBy;
    public string SortedBy => sortedBy;

    private bool isAsc;
    public bool IsAsc => isAsc;

    private CoreWeatherForecastQuery(IQueryable<WeatherForecast> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
                        : base(logManager, timeProvider, dB, currentProfile)
    {
        currentQuery = q;
    }

    public CoreWeatherForecastQuery(IScopeManager logManager,
                                     ITimeProvider timeProvider,
                                     ApplicationDbContext dB,
                                     UserProfile currentProfile)
        : this(from ts in dB.WeatherForecasts
               orderby ts.Date descending
               select ts, logManager, timeProvider, dB, currentProfile)
    {

    }

    public async Task AddAsync(WeatherForecast record)
    {
        DB.WeatherForecasts.Add(record);
        await DB.SaveChangesAsync();
    }

    public IWeatherForecastQuery ByPublicId(string id)
    {
        currentQuery = from pr in currentQuery
                       where pr.PublicId == id
                       select pr;

        return this;
    }

    public IWeatherForecastQuery Clone()
    {
        return new CoreWeatherForecastQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public WeatherForecast CreateNew()
    {
        DateTime now = TimeProvider.CurrentTime;

        return new WeatherForecast
        {
            Id = Guid.NewGuid(),
            PublicId = IdGenerator.New(),
            CreateDate = now,
            UpdateDate = now,
            CreatedBy = CurrentProfile,
            UpdatedBy = CurrentProfile,
        };
    }

    public async Task RemoveAsync(WeatherForecast record)
    {
        DB.WeatherForecasts.Remove(record);
        await DB.SaveChangesAsync();
    }

    public IQueryable<WeatherForecast> Select()
    {
        return (from pr in currentQuery select pr);
    }

    public async Task UpdateAsync(WeatherForecast record)
    {
        DateTime now = TimeProvider.CurrentTime;

        record.UpdateDate = now;
        record.UpdatedBy = CurrentProfile;
        DB.WeatherForecasts.Update(record);
        await DB.SaveChangesAsync();
    }

    public IWeatherForecastQuery Search(string search)
    {
        throw new NotImplementedException();
    }

    public IWeatherForecastQuery Skip(int value)
    {
        currentQuery = currentQuery.Skip(value);
        return this;
    }

    public IWeatherForecastQuery Take(int value)
    {
        currentQuery = currentQuery.Take(value);
        return this;
    }

    public IWeatherForecastQuery SortBy(string column, bool isAsc)
    {
        this.isAsc = isAsc;
        if (String.Compare(column, "Temperature", true) == 0)
        {
            if (isAsc)
            {
                currentQuery = (from ts in currentQuery
                                orderby ts.Temperature
                                select ts);
            }
            else
            {
                currentQuery = (from ts in currentQuery
                                orderby ts.Temperature descending
                                select ts);
            }
            sortedBy = "Temperature";
        }
        else if (String.Compare(column, "Date", true) == 0)
        {
            if (isAsc)
            {
                currentQuery = (from ts in currentQuery
                                orderby ts.Date
                                select ts);
            }
            else
            {
                currentQuery = (from ts in currentQuery
                                orderby ts.Date descending
                                select ts);
            }
            sortedBy = "Date";
        }
        else if (String.Compare(column, "Summary", true) == 0)
        {
            if (isAsc)
            {
                currentQuery = (from ts in currentQuery
                                orderby ts.Summary
                                select ts);
            }
            else
            {
                currentQuery = (from ts in currentQuery
                                orderby ts.Summary descending
                                select ts);
            }
            sortedBy = "Summary";
        }
        else
        {
            throw new ArgumentException("Invalid column name");
        }

        return this;
    }
}
