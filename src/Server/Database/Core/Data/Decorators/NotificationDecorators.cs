using System;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Notifications;
using DevInstance.DevCoreApp.Shared.Model.Common;
using DevInstance.DevCoreApp.Shared.Model.Notifications;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;

public static class NotificationDecorators
{
    public static NotificationItem ToView(this Notification notification)
    {
        return new NotificationItem
        {
            Id = notification.Id.ToString(),
            Type = notification.Type.ToString(),
            Title = notification.Title,
            Message = notification.Message,
            LinkUrl = notification.LinkUrl,
            IsRead = notification.IsRead,
            ReadAt = notification.ReadAt,
            CreateDate = notification.CreateDate,
            GroupKey = notification.GroupKey,
            UserProfileId = notification.User?.PublicId
        };
    }

    public static Notification ToRecord(this Notification notification, NotificationItem item)
    {
        if (Enum.TryParse<NotificationType>(item.Type, out var type))
        {
            notification.Type = type;
        }

        notification.Title = item.Title;
        notification.Message = item.Message;
        notification.LinkUrl = item.LinkUrl;
        notification.IsRead = item.IsRead;
        notification.ReadAt = item.ReadAt;
        notification.GroupKey = item.GroupKey;

        return notification;
    }
}
