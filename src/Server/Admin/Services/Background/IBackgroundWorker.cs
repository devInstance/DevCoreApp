using System;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Background;

public interface IBackgroundWorker
{
    /// <summary>
    /// Persists a BackgroundTask record to the database, then enqueues it for
    /// immediate processing. Preferred over the synchronous Submit() overload.
    /// </summary>
    Task SubmitAsync(BackgroundRequestItem item);

    /// <summary>
    /// Legacy synchronous entry point. Persists and enqueues on a background thread.
    /// Prefer SubmitAsync when possible.
    /// </summary>
    void Submit(BackgroundRequestItem item);

    DateTime? LastHeartbeat { get; }
    int QueueLength { get; }
}
