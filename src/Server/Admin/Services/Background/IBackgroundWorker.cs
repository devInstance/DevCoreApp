using System;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Background;

public interface IBackgroundWorker
{
    void Submit(BackgroundRequestItem dbLog);
    DateTime? LastHeartbeat { get; }
    int QueueLength { get; }
}
