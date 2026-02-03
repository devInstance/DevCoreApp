using DevInstance.WebServiceToolkit.Common.Model;
using System;

namespace DevInstance.DevCoreApp.Shared.Model;

public class EmailLogItem : ModelItem
{
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsHtml { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime? SentDate { get; set; }
    public string? TemplateName { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
}
