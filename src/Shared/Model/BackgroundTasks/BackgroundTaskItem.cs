using DevInstance.WebServiceToolkit.Common.Model;
using System;

namespace DevInstance.DevCoreApp.Shared.Model.BackgroundTasks;

public class BackgroundTaskItem : ModelItem
{
    public string TaskType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Priority { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; }
    public string? ResultReference { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CronExpression { get; set; }
    public string? CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
}
