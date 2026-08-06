namespace DevInstance.DevCoreApp.Server.Admin.Services.Core.Background.Tasks;

public interface IBackgroundTaskHandler
{
    string TaskType { get; }
    Task HandleAsync(string payload, IServiceProvider scopedProvider, CancellationToken cancellationToken);
}
