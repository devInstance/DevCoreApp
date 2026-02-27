using DevInstance.WebServiceToolkit.Common.Model;
using System;

namespace DevInstance.DevCoreApp.Shared.Model.Notifications;

public class NotificationTemplateItem : ModelItem
{
    public string Name { get; set; } = string.Empty;
    public string TitleTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DefaultType { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
}
