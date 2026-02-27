using DevInstance.DevCoreApp.Server.Database.Core.Models.Notifications;
using DevInstance.DevCoreApp.Shared.Model.Notifications;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;

public static class UserNotificationPreferenceDecorators
{
    public static UserNotificationPreferenceItem ToView(this UserNotificationPreference preference)
    {
        return new UserNotificationPreferenceItem
        {
            Id = preference.Id.ToString(),
            NotificationCategory = preference.NotificationCategory,
            InAppEnabled = preference.InAppEnabled,
            EmailEnabled = preference.EmailEnabled,
            UserProfileId = preference.User?.PublicId
        };
    }

    public static UserNotificationPreference ToRecord(this UserNotificationPreference preference, UserNotificationPreferenceItem item)
    {
        preference.NotificationCategory = item.NotificationCategory;
        preference.InAppEnabled = item.InAppEnabled;
        preference.EmailEnabled = item.EmailEnabled;

        return preference;
    }
}
