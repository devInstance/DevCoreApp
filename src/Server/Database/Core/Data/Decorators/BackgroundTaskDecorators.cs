using DevInstance.DevCoreApp.Server.Database.Core.Models.BackgroundTasks;
using DevInstance.DevCoreApp.Shared.Model.BackgroundTasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;

public static class BackgroundTaskDecorators
{
    public static BackgroundTaskItem ToView(this BackgroundTask task)
    {
        return new BackgroundTaskItem
        {
            Id = task.PublicId,
            TaskType = task.TaskType,
            Payload = task.Payload,
            Status = task.Status.ToString(),
            Priority = task.Priority,
            RetryCount = task.RetryCount,
            MaxRetries = task.MaxRetries,
            ResultReference = task.ResultReference,
            ErrorMessage = task.ErrorMessage,
            ScheduledAt = task.ScheduledAt,
            StartedAt = task.StartedAt,
            CompletedAt = task.CompletedAt,
            CronExpression = task.CronExpression,
            CreatedById = task.CreatedBy?.PublicId,
            CreatedByName = task.CreatedBy != null
                ? $"{task.CreatedBy.FirstName} {task.CreatedBy.LastName}".Trim()
                : null,
            CreateDate = task.CreateDate,
            UpdateDate = task.UpdateDate
        };
    }

    public static BackgroundTask ToRecord(this BackgroundTask task, BackgroundTaskItem item)
    {
        task.TaskType = item.TaskType;
        task.Payload = item.Payload;
        task.Priority = item.Priority;
        task.MaxRetries = item.MaxRetries;
        task.ResultReference = item.ResultReference;
        task.ErrorMessage = item.ErrorMessage;
        task.ScheduledAt = item.ScheduledAt;
        task.CronExpression = item.CronExpression;

        return task;
    }
}
