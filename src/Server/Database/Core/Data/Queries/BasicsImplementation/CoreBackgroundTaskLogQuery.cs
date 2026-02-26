using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Core.Models.BackgroundTasks;
using DevInstance.DevCoreApp.Shared.Model.Common;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using System;
using System.Linq;

namespace NoCrast.Server.Database.Postgres.Data.Queries;

public class CoreBackgroundTaskLogQuery : CoreBaseQuery<BackgroundTaskLog, CoreBackgroundTaskLogQuery>, IBackgroundTaskLogQuery
{
    private CoreBackgroundTaskLogQuery(IQueryable<BackgroundTaskLog> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreBackgroundTaskLogQuery(IScopeManager logManager,
                             ITimeProvider timeProvider,
                             ApplicationDbContext dB,
                             UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public IBackgroundTaskLogQuery ByPublicId(string id) => ByGuidIdHelper(id);

    public IBackgroundTaskLogQuery ByBackgroundTaskId(Guid backgroundTaskId)
    {
        currentQuery = from btl in currentQuery
                       where btl.BackgroundTaskId == backgroundTaskId
                       select btl;
        return this;
    }

    public IBackgroundTaskLogQuery ByStatus(BackgroundTaskLogStatus status)
    {
        currentQuery = from btl in currentQuery
                       where btl.Status == status
                       select btl;
        return this;
    }

    public IBackgroundTaskLogQuery Clone()
    {
        return new CoreBackgroundTaskLogQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public BackgroundTaskLog CreateNew()
    {
        return new BackgroundTaskLog
        {
            Id = Guid.NewGuid(),
            StartedAt = TimeProvider.CurrentTime,
        };
    }

    public IBackgroundTaskLogQuery Skip(int value) => SkipHelper(value);

    public IBackgroundTaskLogQuery Take(int value) => TakeHelper(value);

    public IBackgroundTaskLogQuery SortBy(string column, bool isAsc)
    {
        this.IsAsc = isAsc;

        if (string.Compare(column, "startedat", true) == 0)
        {
            currentQuery = isAsc
                ? (from btl in currentQuery orderby btl.StartedAt select btl)
                : (from btl in currentQuery orderby btl.StartedAt descending select btl);
            SortedBy = "startedat";
        }
        else if (string.Compare(column, "completedat", true) == 0)
        {
            currentQuery = isAsc
                ? (from btl in currentQuery orderby btl.CompletedAt select btl)
                : (from btl in currentQuery orderby btl.CompletedAt descending select btl);
            SortedBy = "completedat";
        }
        else if (string.Compare(column, "attempt", true) == 0)
        {
            currentQuery = isAsc
                ? (from btl in currentQuery orderby btl.Attempt select btl)
                : (from btl in currentQuery orderby btl.Attempt descending select btl);
            SortedBy = "attempt";
        }
        else if (string.Compare(column, "status", true) == 0)
        {
            currentQuery = isAsc
                ? (from btl in currentQuery orderby btl.Status select btl)
                : (from btl in currentQuery orderby btl.Status descending select btl);
            SortedBy = "status";
        }
        else
        {
            throw new ArgumentException("Invalid column name");
        }

        return this;
    }
}
