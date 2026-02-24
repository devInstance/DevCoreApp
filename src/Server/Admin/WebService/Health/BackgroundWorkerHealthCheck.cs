using DevInstance.DevCoreApp.Server.Admin.Services.Background;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Health;

public class BackgroundWorkerHealthCheck : IHealthCheck
{
    private readonly IBackgroundWorker _worker;
    private static readonly TimeSpan HeartbeatThreshold = TimeSpan.FromMinutes(2);

    public BackgroundWorkerHealthCheck(IBackgroundWorker worker)
    {
        _worker = worker;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            { "queueLength", _worker.QueueLength }
        };

        if (_worker.LastHeartbeat == null)
        {
            data["lastHeartbeat"] = "never";
            return Task.FromResult(HealthCheckResult.Degraded(
                "Background worker has not started yet.", data: data));
        }

        var elapsed = DateTime.UtcNow - _worker.LastHeartbeat.Value;
        data["lastHeartbeat"] = _worker.LastHeartbeat.Value.ToString("O");
        data["elapsedSeconds"] = elapsed.TotalSeconds;

        if (elapsed > HeartbeatThreshold)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Background worker heartbeat is stale ({elapsed.TotalSeconds:F0}s ago).", data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            "Background worker is running.", data: data));
    }
}
