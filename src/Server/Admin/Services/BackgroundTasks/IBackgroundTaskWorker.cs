namespace DevInstance.DevCoreApp.Server.Admin.Services.BackgroundTasks;

public interface IBackgroundTaskWorker
{
    void Enqueue(Guid backgroundTaskId);
    DateTime? LastHeartbeat { get; }
    int QueueLength { get; }
    Task ExecuteAsync(CancellationToken stoppingToken);
}
