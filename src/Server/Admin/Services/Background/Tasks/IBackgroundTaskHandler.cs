namespace DevInstance.DevCoreApp.Server.Admin.Services.Background.Tasks;

public interface IBackgroundTaskHandler
{
    string TaskType { get; }
    Task HandleAsync(string payload, IServiceProvider scopedProvider, CancellationToken cancellationToken);
}
