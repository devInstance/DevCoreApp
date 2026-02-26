namespace DevInstance.DevCoreApp.Shared.Model.Common;

public enum BackgroundTaskStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3
}

public enum BackgroundTaskLogStatus
{
    Running = 0,
    Completed = 1,
    Failed = 2
}
