using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using DevInstance.DevCoreApp.Shared.Model.Common;
using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models.BackgroundTasks;

public class BackgroundTaskLog : DatabaseBaseObject
{
    public Guid BackgroundTaskId { get; set; }
    public BackgroundTask BackgroundTask { get; set; }

    public int Attempt { get; set; }

    public BackgroundTaskLogStatus Status { get; set; }

    public string? Message { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
