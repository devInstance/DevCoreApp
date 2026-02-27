using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using DevInstance.DevCoreApp.Shared.Model.Common;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models.Notifications;

public class NotificationTemplate : DatabaseObject
{
    public string Name { get; set; } = string.Empty;

    public string TitleTemplate { get; set; } = string.Empty;

    public string BodyTemplate { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public NotificationType DefaultType { get; set; }
}
