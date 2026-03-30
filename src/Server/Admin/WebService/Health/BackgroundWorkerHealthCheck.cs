using DevInstance.DevCoreApp.Server.Admin.Services.Background;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Shared.Model.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Health;

public class BackgroundWorkerHealthCheck : IHealthCheck
{
    private readonly IBackgroundWorker _worker;
    private readonly ApplicationDbContext _dbContext;
    private static readonly TimeSpan HeartbeatThreshold = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan StaleRunningThreshold = TimeSpan.FromMinutes(15);

    public BackgroundWorkerHealthCheck(IBackgroundWorker worker, ApplicationDbContext dbContext)
    {
        _worker = worker;
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var staleRunningCutoff = now - StaleRunningThreshold;
        var dbQueuedCount = await _dbContext.BackgroundTasks
            .Where(t => t.Status == BackgroundTaskStatus.Queued)
            .CountAsync(cancellationToken);
        var dbDueQueuedCount = await _dbContext.BackgroundTasks
            .Where(t => t.Status == BackgroundTaskStatus.Queued && t.ScheduledAt <= now)
            .CountAsync(cancellationToken);
        var dbRunningCount = await _dbContext.BackgroundTasks
            .Where(t => t.Status == BackgroundTaskStatus.Running)
            .CountAsync(cancellationToken);
        var dbStaleRunningCount = await _dbContext.BackgroundTasks
            .Where(t => t.Status == BackgroundTaskStatus.Running &&
                (!t.StartedAt.HasValue || t.StartedAt < staleRunningCutoff))
            .CountAsync(cancellationToken);

        var data = new Dictionary<string, object>
        {
            { "localImmediateQueueLength", _worker.QueueLength },
            { "dbQueuedCount", dbQueuedCount },
            { "dbDueQueuedCount", dbDueQueuedCount },
            { "dbRunningCount", dbRunningCount },
            { "dbStaleRunningCount", dbStaleRunningCount }
        };

        if (_worker.LastHeartbeat == null)
        {
            data["lastHeartbeat"] = "never";
            return HealthCheckResult.Degraded(
                "Background worker has not started yet.", data: data);
        }

        var elapsed = DateTime.UtcNow - _worker.LastHeartbeat.Value;
        data["lastHeartbeat"] = _worker.LastHeartbeat.Value.ToString("O");
        data["elapsedSeconds"] = elapsed.TotalSeconds;

        if (elapsed > HeartbeatThreshold)
        {
            return HealthCheckResult.Unhealthy(
                $"Background worker heartbeat is stale ({elapsed.TotalSeconds:F0}s ago).", data: data);
        }

        if (dbStaleRunningCount > 0)
        {
            return HealthCheckResult.Degraded(
                $"Background worker is running, but {dbStaleRunningCount} stale running task(s) were detected.",
                data: data);
        }

        return HealthCheckResult.Healthy(
            "Background worker is running.", data: data);
    }
}
