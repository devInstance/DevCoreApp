﻿using DevInstance.LogScope;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using System;
using System.Linq;

namespace NoCrast.Server.Database.Postgres.Data.Queries;

public class CoreWeatherForecastQuery : CoreBaseQuery, IWeatherForecastQuery
{
    private IQueryable<WeatherForecast> currentQuery;

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

    public void Add(WeatherForecast record)
    {
        DB.WeatherForecasts.Add(record);
        DB.SaveChanges();
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

    public void Remove(WeatherForecast record)
    {
        DB.WeatherForecasts.Remove(record);
        DB.SaveChanges();
    }

    public IQueryable<WeatherForecast> Select()
    {
        return (from pr in currentQuery select pr);
    }

    public void Update(WeatherForecast record)
    {
        DateTime now = TimeProvider.CurrentTime;

        record.UpdateDate = now;
        record.UpdatedBy = CurrentProfile;
        DB.WeatherForecasts.Update(record);
        DB.SaveChanges();
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
}
