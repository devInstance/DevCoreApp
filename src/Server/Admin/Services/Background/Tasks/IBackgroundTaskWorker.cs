namespace DevInstance.DevCoreApp.Server.Admin.Services.Background.Tasks;

public interface IBackgroundTaskWorker
{
    void Enqueue(Guid backgroundTaskId);
    DateTime? LastHeartbeat { get; }
    int QueueLength { get; }
    Task ExecuteAsync(CancellationToken stoppingToken);
}
