using DevInstance.DevCoreApp.Server.Database.Core.Models.BackgroundTasks;
using DevInstance.DevCoreApp.Shared.Model.BackgroundTasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;

public static class BackgroundTaskLogDecorators
{
    public static BackgroundTaskLogItem ToView(this BackgroundTaskLog log)
    {
        return new BackgroundTaskLogItem
        {
            Id = log.Id.ToString(),
            BackgroundTaskId = log.BackgroundTask?.PublicId ?? string.Empty,
            Attempt = log.Attempt,
            Status = log.Status.ToString(),
            Message = log.Message,
            ErrorMessage = log.ErrorMessage,
            StartedAt = log.StartedAt,
            CompletedAt = log.CompletedAt
        };
    }

    public static BackgroundTaskLog ToRecord(this BackgroundTaskLog log, BackgroundTaskLogItem item)
    {
        log.Attempt = item.Attempt;
        log.Message = item.Message;
        log.ErrorMessage = item.ErrorMessage;
        log.StartedAt = item.StartedAt;
        log.CompletedAt = item.CompletedAt;

        return log;
    }
}
