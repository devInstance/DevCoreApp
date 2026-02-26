using DevInstance.BlazorToolkit.Utils;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Core.Models.BackgroundTasks;
using DevInstance.DevCoreApp.Shared.Model.Common;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NoCrast.Server.Database.Postgres.Data.Queries;

public class CoreBackgroundTaskQuery : CoreBaseQuery, IBackgroundTaskQuery
{
    private IQueryable<BackgroundTask> currentQuery;

    public string SortedBy { get; set; }

    public bool IsAsc { get; set; }

    private CoreBackgroundTaskQuery(IQueryable<BackgroundTask> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
        currentQuery = q;
    }

    public CoreBackgroundTaskQuery(IScopeManager logManager,
                             ITimeProvider timeProvider,
                             ApplicationDbContext dB,
                             UserProfile currentProfile)
        : this(from bt in dB.BackgroundTasks select bt, logManager, timeProvider, dB, currentProfile)
    {
    }

    public async Task AddAsync(BackgroundTask record)
    {
        DB.BackgroundTasks.Add(record);
        await DB.SaveChangesAsync();
    }

    public IBackgroundTaskQuery ByPublicId(string id)
    {
        currentQuery = from bt in currentQuery
                       where bt.PublicId == id
                       select bt;
        return this;
    }

    public IBackgroundTaskQuery ByStatus(BackgroundTaskStatus status)
    {
        currentQuery = from bt in currentQuery
                       where bt.Status == status
                       select bt;
        return this;
    }

    public IBackgroundTaskQuery ByTaskType(string taskType)
    {
        currentQuery = from bt in currentQuery
                       where bt.TaskType == taskType
                       select bt;
        return this;
    }

    public IBackgroundTaskQuery ByDateRange(DateTime? start, DateTime? end)
    {
        if (start.HasValue)
        {
            currentQuery = from bt in currentQuery
                           where bt.ScheduledAt >= start.Value
                           select bt;
        }
        if (end.HasValue)
        {
            currentQuery = from bt in currentQuery
                           where bt.ScheduledAt <= end.Value
                           select bt;
        }
        return this;
    }

    public IBackgroundTaskQuery ByCreatedById(Guid createdById)
    {
        currentQuery = from bt in currentQuery
                       where bt.CreatedById == createdById
                       select bt;
        return this;
    }

    public IBackgroundTaskQuery Clone()
    {
        return new CoreBackgroundTaskQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public BackgroundTask CreateNew()
    {
        DateTime now = TimeProvider.CurrentTime;

        return new BackgroundTask
        {
            Id = Guid.NewGuid(),
            PublicId = IdGenerator.New(),
            CreateDate = now,
            UpdateDate = now,
        };
    }

    public async Task RemoveAsync(BackgroundTask record)
    {
        DB.BackgroundTasks.Remove(record);
        await DB.SaveChangesAsync();
    }

    public IQueryable<BackgroundTask> Select()
    {
        return currentQuery;
    }

    public async Task UpdateAsync(BackgroundTask record)
    {
        DateTime now = TimeProvider.CurrentTime;
        record.UpdateDate = now;
        DB.BackgroundTasks.Update(record);
        await DB.SaveChangesAsync();
    }

    public IBackgroundTaskQuery Search(string search)
    {
        currentQuery = from bt in currentQuery
                       where bt.TaskType.IndexOf(search) >= 0 ||
                             (bt.ResultReference != null && bt.ResultReference.IndexOf(search) >= 0) ||
                             (bt.ErrorMessage != null && bt.ErrorMessage.IndexOf(search) >= 0)
                       select bt;
        return this;
    }

    public IBackgroundTaskQuery Skip(int value)
    {
        currentQuery = currentQuery.Skip(value);
        return this;
    }

    public IBackgroundTaskQuery Take(int value)
    {
        currentQuery = currentQuery.Take(value);
        return this;
    }

    public IBackgroundTaskQuery SortBy(string column, bool isAsc)
    {
        this.IsAsc = isAsc;

        if (string.Compare(column, "scheduledat", true) == 0)
        {
            currentQuery = isAsc
                ? (from bt in currentQuery orderby bt.ScheduledAt select bt)
                : (from bt in currentQuery orderby bt.ScheduledAt descending select bt);
            SortedBy = "scheduledat";
        }
        else if (string.Compare(column, "startedat", true) == 0)
        {
            currentQuery = isAsc
                ? (from bt in currentQuery orderby bt.StartedAt select bt)
                : (from bt in currentQuery orderby bt.StartedAt descending select bt);
            SortedBy = "startedat";
        }
        else if (string.Compare(column, "completedat", true) == 0)
        {
            currentQuery = isAsc
                ? (from bt in currentQuery orderby bt.CompletedAt select bt)
                : (from bt in currentQuery orderby bt.CompletedAt descending select bt);
            SortedBy = "completedat";
        }
        else if (string.Compare(column, "tasktype", true) == 0)
        {
            currentQuery = isAsc
                ? (from bt in currentQuery orderby bt.TaskType select bt)
                : (from bt in currentQuery orderby bt.TaskType descending select bt);
            SortedBy = "tasktype";
        }
        else if (string.Compare(column, "status", true) == 0)
        {
            currentQuery = isAsc
                ? (from bt in currentQuery orderby bt.Status select bt)
                : (from bt in currentQuery orderby bt.Status descending select bt);
            SortedBy = "status";
        }
        else if (string.Compare(column, "priority", true) == 0)
        {
            currentQuery = isAsc
                ? (from bt in currentQuery orderby bt.Priority select bt)
                : (from bt in currentQuery orderby bt.Priority descending select bt);
            SortedBy = "priority";
        }
        else
        {
            throw new ArgumentException("Invalid column name");
        }

        return this;
    }
}
