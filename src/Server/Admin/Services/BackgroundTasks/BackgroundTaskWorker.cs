using System.Collections.Concurrent;
using DevInstance.DevCoreApp.Server.Admin.Services.Background;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models.BackgroundTasks;
using DevInstance.DevCoreApp.Shared.Model.Common;
using DevInstance.LogScope;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DevInstance.DevCoreApp.Server.Admin.Services.BackgroundTasks;

public class BackgroundTaskWorker : IBackgroundTaskWorker
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IScopeLog _log;
    private readonly BackgroundTaskSettings _settings;
    private readonly ConcurrentQueue<Guid> _immediateQueue = new();
    private readonly Dictionary<string, IBackgroundTaskHandler> _handlers;
    private SemaphoreSlim _concurrencySemaphore = null!;

    public DateTime? LastHeartbeat { get; private set; }
    public int QueueLength => _immediateQueue.Count;

    public BackgroundTaskWorker(
        IServiceScopeFactory scopeFactory,
        IScopeManager logManager,
        IOptions<BackgroundTaskSettings> settings,
        IEnumerable<IBackgroundTaskHandler> handlers)
    {
        _scopeFactory = scopeFactory;
        _log = logManager.CreateLogger(this);
        _settings = settings.Value;
        _handlers = handlers.ToDictionary(h => h.TaskType, h => h);
    }

    public void Enqueue(Guid backgroundTaskId)
    {
        _immediateQueue.Enqueue(backgroundTaskId);
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _concurrencySemaphore = new SemaphoreSlim(_settings.MaxConcurrency, _settings.MaxConcurrency);

        await RecoverStuckTasksAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            LastHeartbeat = DateTime.UtcNow;

            try
            {
                var taskIds = await ClaimQueuedTasksAsync(stoppingToken);

                if (taskIds.Count > 0)
                {
                    var runningTasks = new List<Task>();
                    foreach (var taskId in taskIds)
                    {
                        await _concurrencySemaphore.WaitAsync(stoppingToken);
                        runningTasks.Add(ProcessTaskWithSemaphoreAsync(taskId, stoppingToken));
                    }

                    await Task.WhenAll(runningTasks);
                }
                else
                {
                    await Task.Delay(_settings.PollingIntervalSeconds * 1000, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.E($"Background task polling error: {ex.Message}");
                await Task.Delay(_settings.PollingIntervalSeconds * 1000, stoppingToken);
            }
        }
    }

    private async Task RecoverStuckTasksAsync()
    {
        using var l = _log.TraceScope();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var operationContext = scope.ServiceProvider.GetRequiredService<BackgroundOperationContext>();
            operationContext.Reset();

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var resetCount = await db.BackgroundTasks
                .Where(t => t.Status == BackgroundTaskStatus.Running)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.Status, BackgroundTaskStatus.Queued)
                    .SetProperty(t => t.StartedAt, (DateTime?)null));

            if (resetCount > 0)
            {
                l.I($"Crash recovery: reset {resetCount} stuck Running task(s) to Queued.");
            }
        }
        catch (Exception ex)
        {
            l.E($"Crash recovery failed: {ex.Message}");
        }
    }

    private async Task<List<Guid>> ClaimQueuedTasksAsync(CancellationToken cancellationToken)
    {
        var claimed = new List<Guid>();

        // Drain immediate queue first
        while (_immediateQueue.TryDequeue(out var immediateId))
        {
            claimed.Add(immediateId);
        }

        // Poll database for queued tasks
        using var scope = _scopeFactory.CreateScope();
        var operationContext = scope.ServiceProvider.GetRequiredService<BackgroundOperationContext>();
        operationContext.Reset();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;

        var candidates = await db.BackgroundTasks
            .Where(t => t.Status == BackgroundTaskStatus.Queued && t.ScheduledAt <= now)
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.CreateDate)
            .Take(_settings.BatchSize)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        foreach (var candidateId in candidates)
        {
            // Atomic claim: only update if still Queued (prevents double-processing)
            var updated = await db.BackgroundTasks
                .Where(t => t.Id == candidateId && t.Status == BackgroundTaskStatus.Queued)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.Status, BackgroundTaskStatus.Running)
                    .SetProperty(t => t.StartedAt, now), cancellationToken);

            if (updated > 0)
            {
                claimed.Add(candidateId);
            }
        }

        return claimed;
    }

    private async Task ProcessTaskWithSemaphoreAsync(Guid taskId, CancellationToken cancellationToken)
    {
        try
        {
            await ProcessTaskAsync(taskId, cancellationToken);
        }
        finally
        {
            _concurrencySemaphore.Release();
        }
    }

    private async Task ProcessTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var operationContext = scope.ServiceProvider.GetRequiredService<BackgroundOperationContext>();
        operationContext.Reset();

        var repository = scope.ServiceProvider.GetRequiredService<IQueryRepository>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var task = await db.BackgroundTasks.FindAsync([taskId], cancellationToken);
        if (task == null)
        {
            _log.E($"Background task {taskId} not found.");
            return;
        }

        if (!_handlers.TryGetValue(task.TaskType, out var handler))
        {
            _log.E($"No handler registered for task type '{task.TaskType}'.");
            await FailTaskAsync(db, task, $"No handler registered for task type '{task.TaskType}'.", cancellationToken);
            return;
        }

        // Create log entry for this attempt
        var taskLogQuery = repository.GetBackgroundTaskLogQuery(null!);
        var taskLog = taskLogQuery.CreateNew();
        taskLog.BackgroundTaskId = task.Id;
        taskLog.Attempt = task.RetryCount + 1;
        taskLog.Status = BackgroundTaskLogStatus.Running;
        await taskLogQuery.AddAsync(taskLog);

        try
        {
            await handler.HandleAsync(task.Payload, scope.ServiceProvider, cancellationToken);

            // Mark task completed
            task.Status = BackgroundTaskStatus.Completed;
            task.CompletedAt = DateTime.UtcNow;
            task.ErrorMessage = null;
            db.BackgroundTasks.Update(task);
            await db.SaveChangesAsync(cancellationToken);

            // Mark log completed
            taskLog.Status = BackgroundTaskLogStatus.Completed;
            taskLog.CompletedAt = DateTime.UtcNow;
            await taskLogQuery.UpdateAsync(taskLog);
        }
        catch (Exception ex)
        {
            _log.E($"Background task {task.PublicId} (type={task.TaskType}) failed: {ex.Message}");

            // Update log entry
            taskLog.Status = BackgroundTaskLogStatus.Failed;
            taskLog.ErrorMessage = ex.Message;
            taskLog.CompletedAt = DateTime.UtcNow;
            var logUpdateQuery = repository.GetBackgroundTaskLogQuery(null!);
            await logUpdateQuery.UpdateAsync(taskLog);

            // Retry or fail
            task.RetryCount++;
            task.ErrorMessage = ex.Message;

            if (task.RetryCount < task.MaxRetries)
            {
                // Exponential backoff: baseDelay * 2^(retryCount-1), capped at maxDelay
                var delaySeconds = Math.Min(
                    _settings.BaseRetryDelaySeconds * Math.Pow(2, task.RetryCount - 1),
                    _settings.MaxRetryDelaySeconds);
                task.Status = BackgroundTaskStatus.Queued;
                task.ScheduledAt = DateTime.UtcNow.AddSeconds(delaySeconds);
                task.StartedAt = null;

                _log.I($"Background task {task.PublicId} re-queued for retry {task.RetryCount}/{task.MaxRetries} in {delaySeconds:F0}s.");
            }
            else
            {
                task.Status = BackgroundTaskStatus.Failed;
                task.CompletedAt = DateTime.UtcNow;
            }

            db.BackgroundTasks.Update(task);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task FailTaskAsync(ApplicationDbContext db, BackgroundTask task, string errorMessage, CancellationToken cancellationToken)
    {
        task.Status = BackgroundTaskStatus.Failed;
        task.ErrorMessage = errorMessage;
        task.CompletedAt = DateTime.UtcNow;
        db.BackgroundTasks.Update(task);
        await db.SaveChangesAsync(cancellationToken);
    }
}
