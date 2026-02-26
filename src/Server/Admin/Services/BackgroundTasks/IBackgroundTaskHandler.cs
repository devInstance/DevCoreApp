namespace DevInstance.DevCoreApp.Server.Admin.Services.BackgroundTasks;

public interface IBackgroundTaskHandler
{
    string TaskType { get; }
    Task HandleAsync(string payload, IServiceProvider scopedProvider, CancellationToken cancellationToken);
}
