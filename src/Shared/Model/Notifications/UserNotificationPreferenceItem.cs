using DevInstance.WebServiceToolkit.Common.Model;

namespace DevInstance.DevCoreApp.Shared.Model.Notifications;

public class UserNotificationPreferenceItem : ModelItem
{
    public string NotificationCategory { get; set; } = string.Empty;
    public bool InAppEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public string? UserProfileId { get; set; }
}
