using Microsoft.Extensions.Hosting;

namespace DevInstance.DevCoreApp.Server.Admin.Services.BackgroundTasks;

public class BackgroundTaskHostedService : BackgroundService
{
    private readonly IBackgroundTaskWorker _worker;

    public BackgroundTaskHostedService(IBackgroundTaskWorker worker)
    {
        _worker = worker;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _worker.ExecuteAsync(stoppingToken);
    }
}
