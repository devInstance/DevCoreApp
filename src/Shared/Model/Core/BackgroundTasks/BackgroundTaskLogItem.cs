using DevInstance.WebServiceToolkit.Common.Model;
using System;

namespace DevInstance.DevCoreApp.Shared.Model.Core.BackgroundTasks;

public class BackgroundTaskLogItem : IModelItem
{
    public string Id { get; set; }

    public string BackgroundTaskId { get; set; } = string.Empty;
    public int Attempt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
