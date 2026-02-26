using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using DevInstance.DevCoreApp.Shared.Model.Common;
using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models.BackgroundTasks;

public class BackgroundTask : DatabaseObject, IOrganizationScoped
{
    public Guid OrganizationId { get; set; }

    public string TaskType { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public BackgroundTaskStatus Status { get; set; }

    public int Priority { get; set; }

    public int RetryCount { get; set; }

    public int MaxRetries { get; set; }

    public string? ResultReference { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime ScheduledAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? CronExpression { get; set; }

    public Guid CreatedById { get; set; }
    public UserProfile CreatedBy { get; set; }

    public Organization Organization { get; set; }
}
