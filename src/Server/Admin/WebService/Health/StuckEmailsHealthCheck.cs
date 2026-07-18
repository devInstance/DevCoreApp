using DevInstance.DevCoreApp.Server.Database.Core.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Health;

public class StuckEmailsHealthCheck : IHealthCheck
{
    private readonly IQueryRepository _repository;
    private static readonly TimeSpan StuckThreshold = TimeSpan.FromMinutes(30);

    public StuckEmailsHealthCheck(IQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoff = DateTime.UtcNow - StuckThreshold;
            var stuckCount = await _repository.GetEmailLogQuery(null!)
                .CountStuckQueuedAsync(cutoff, cancellationToken);

            var data = new Dictionary<string, object>
            {
                { "stuckCount", stuckCount },
                { "thresholdMinutes", StuckThreshold.TotalMinutes }
            };

            if (stuckCount > 0)
            {
                return HealthCheckResult.Degraded(
                    $"{stuckCount} email(s) stuck in Queued status for over {StuckThreshold.TotalMinutes} minutes.",
                    data: data);
            }

            return HealthCheckResult.Healthy("No stuck emails.", data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Stuck emails check failed.", ex);
        }
    }
}
