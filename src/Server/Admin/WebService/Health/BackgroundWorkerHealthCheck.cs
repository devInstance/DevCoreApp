using DevInstance.DevCoreApp.Server.Admin.Services.Core.Background;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Health;

public class BackgroundWorkerHealthCheck : IHealthCheck
{
    private readonly IBackgroundWorker _worker;
    private readonly IQueryRepository _repository;
    private static readonly TimeSpan HeartbeatThreshold = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan StaleRunningThreshold = TimeSpan.FromMinutes(15);

    public BackgroundWorkerHealthCheck(IBackgroundWorker worker, IQueryRepository repository)
    {
        _worker = worker;
        _repository = repository;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var staleRunningCutoff = now - StaleRunningThreshold;
        var taskQuery = _repository.GetBackgroundTaskQuery(null!);
        var dbQueuedCount = await taskQuery.CountByStatusAsync(BackgroundTaskStatus.Queued, cancellationToken);
        var dbDueQueuedCount = await taskQuery.CountDueQueuedAsync(now, cancellationToken);
        var dbRunningCount = await taskQuery.CountByStatusAsync(BackgroundTaskStatus.Running, cancellationToken);
        var dbStaleRunningCount = await taskQuery.CountStaleRunningAsync(staleRunningCutoff, cancellationToken);

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
