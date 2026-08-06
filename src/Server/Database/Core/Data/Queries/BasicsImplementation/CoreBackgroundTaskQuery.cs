using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Core.Models.BackgroundTasks;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Utils.Core;
using DevInstance.LogScope;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public class CoreBackgroundTaskQuery : CoreDatabaseObjectQuery<BackgroundTask, CoreBackgroundTaskQuery>, IBackgroundTaskQuery
{
    private CoreBackgroundTaskQuery(IQueryable<BackgroundTask> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreBackgroundTaskQuery(IScopeManager logManager,
                             ITimeProvider timeProvider,
                             ApplicationDbContext dB,
                             UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public IBackgroundTaskQuery ByPublicId(string id) => ByPublicIdHelper(id);

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

    public IBackgroundTaskQuery Search(string search)
    {
        currentQuery = from bt in currentQuery
                       where bt.TaskType.IndexOf(search) >= 0 ||
                             (bt.ResultReference != null && bt.ResultReference.IndexOf(search) >= 0) ||
                             (bt.ErrorMessage != null && bt.ErrorMessage.IndexOf(search) >= 0)
                       select bt;
        return this;
    }

    public IBackgroundTaskQuery Skip(int value) => SkipHelper(value);

    public IBackgroundTaskQuery Take(int value) => TakeHelper(value);

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

    public async Task<int> RecoverStuckRunningAsync(DateTime timeoutCutoff, DateTime now)
    {
        return await DB.BackgroundTasks
            .Where(t => t.Status == BackgroundTaskStatus.Running &&
                (!t.StartedAt.HasValue || t.StartedAt < timeoutCutoff))
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, BackgroundTaskStatus.Queued)
                .SetProperty(t => t.StartedAt, (DateTime?)null)
                .SetProperty(t => t.ScheduledAt, now));
    }

    public async Task<IReadOnlyList<Guid>> SelectQueuedCandidateIdsAsync(DateTime now, int batchSize, CancellationToken cancellationToken)
    {
        return await DB.BackgroundTasks
            .Where(t => t.Status == BackgroundTaskStatus.Queued && t.ScheduledAt <= now)
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.CreateDate)
            .Take(batchSize)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryClaimAsync(Guid id, DateTime now, CancellationToken cancellationToken)
    {
        var updated = await DB.BackgroundTasks
            .Where(t => t.Id == id && t.Status == BackgroundTaskStatus.Queued)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, BackgroundTaskStatus.Running)
                .SetProperty(t => t.StartedAt, now), cancellationToken);

        return updated > 0;
    }

    public async Task<BackgroundTask?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await DB.BackgroundTasks.FindAsync([id], cancellationToken);
    }

    public async Task<int> CountByStatusAsync(BackgroundTaskStatus status, CancellationToken cancellationToken)
    {
        return await DB.BackgroundTasks.CountAsync(t => t.Status == status, cancellationToken);
    }

    public async Task<int> CountDueQueuedAsync(DateTime now, CancellationToken cancellationToken)
    {
        return await DB.BackgroundTasks
            .Where(t => t.Status == BackgroundTaskStatus.Queued && t.ScheduledAt <= now)
            .CountAsync(cancellationToken);
    }

    public async Task<int> CountStaleRunningAsync(DateTime staleRunningCutoff, CancellationToken cancellationToken)
    {
        return await DB.BackgroundTasks
            .Where(t => t.Status == BackgroundTaskStatus.Running &&
                (!t.StartedAt.HasValue || t.StartedAt < staleRunningCutoff))
            .CountAsync(cancellationToken);
    }
}
