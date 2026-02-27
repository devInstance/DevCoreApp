using System;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Notifications;
using DevInstance.DevCoreApp.Shared.Model.Common;
using DevInstance.DevCoreApp.Shared.Model.Notifications;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;

public static class NotificationTemplateDecorators
{
    public static NotificationTemplateItem ToView(this NotificationTemplate template)
    {
        return new NotificationTemplateItem
        {
            Id = template.PublicId,
            Name = template.Name,
            TitleTemplate = template.TitleTemplate,
            BodyTemplate = template.BodyTemplate,
            Category = template.Category,
            DefaultType = template.DefaultType.ToString(),
            CreateDate = template.CreateDate,
            UpdateDate = template.UpdateDate
        };
    }

    public static NotificationTemplate ToRecord(this NotificationTemplate template, NotificationTemplateItem item)
    {
        template.Name = item.Name;
        template.TitleTemplate = item.TitleTemplate;
        template.BodyTemplate = item.BodyTemplate;
        template.Category = item.Category;

        if (Enum.TryParse<NotificationType>(item.DefaultType, out var defaultType))
        {
            template.DefaultType = defaultType;
        }

        return template;
    }
}
