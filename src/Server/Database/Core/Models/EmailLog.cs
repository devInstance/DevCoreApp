using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models;

public enum EmailLogStatus
{
    Batched = 0,
    Sent = 1,
    Failed = 2
}

public class EmailLog : DatabaseObject
{
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsHtml { get; set; }
    public EmailLogStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime? SentDate { get; set; }
    public string? TemplateName { get; set; }
}
