using DevInstance.DevCoreApp.Server.Database.Core.Models.BackgroundTasks;
using DevInstance.DevCoreApp.Shared.Model.Common;
using DevInstance.WebServiceToolkit.Database.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IBackgroundTaskQuery : IModelQuery<BackgroundTask, IBackgroundTaskQuery>,
        IQSearchable<IBackgroundTaskQuery>,
        IQPageable<IBackgroundTaskQuery>,
        IQSortable<IBackgroundTaskQuery>
{
    IQueryable<BackgroundTask> Select();

    IBackgroundTaskQuery ByStatus(BackgroundTaskStatus status);
    IBackgroundTaskQuery ByTaskType(string taskType);
    IBackgroundTaskQuery ByDateRange(DateTime? start, DateTime? end);
    IBackgroundTaskQuery ByCreatedById(Guid createdById);

    /// <summary>
    /// Resets Running tasks that have exceeded the timeout (or never recorded a start) back to
    /// Queued in a single bulk update. Returns the number of tasks recovered.
    /// </summary>
    Task<int> RecoverStuckRunningAsync(DateTime timeoutCutoff, DateTime now);

    /// <summary>
    /// Returns the ids of due, Queued tasks ordered by priority then creation, capped at batchSize.
    /// </summary>
    Task<IReadOnlyList<Guid>> SelectQueuedCandidateIdsAsync(DateTime now, int batchSize, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically claims a task: flips it Queued → Running only if still Queued. Returns true if claimed.
    /// </summary>
    Task<bool> TryClaimAsync(Guid id, DateTime now, CancellationToken cancellationToken);

    /// <summary>Loads a single task by its internal id (tracked), or null if missing.</summary>
    Task<BackgroundTask?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<int> CountByStatusAsync(BackgroundTaskStatus status, CancellationToken cancellationToken);
    Task<int> CountDueQueuedAsync(DateTime now, CancellationToken cancellationToken);
    Task<int> CountStaleRunningAsync(DateTime staleRunningCutoff, CancellationToken cancellationToken);
}
