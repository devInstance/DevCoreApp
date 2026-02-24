using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Health;

public class StuckEmailsHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _dbContext;
    private static readonly TimeSpan StuckThreshold = TimeSpan.FromMinutes(30);

    public StuckEmailsHealthCheck(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoff = DateTime.UtcNow - StuckThreshold;
            var stuckCount = await _dbContext.EmailLogs
                .Where(e => e.Status == EmailLogStatus.Batched && e.CreateDate < cutoff)
                .CountAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                { "stuckCount", stuckCount },
                { "thresholdMinutes", StuckThreshold.TotalMinutes }
            };

            if (stuckCount > 0)
            {
                return HealthCheckResult.Degraded(
                    $"{stuckCount} email(s) stuck in Batched status for over {StuckThreshold.TotalMinutes} minutes.",
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
