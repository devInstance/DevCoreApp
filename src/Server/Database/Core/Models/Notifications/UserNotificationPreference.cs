using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models.Notifications;

public class UserNotificationPreference : DatabaseBaseObject, IOrganizationScoped
{
    public Guid OrganizationId { get; set; }

    public Guid UserProfileId { get; set; }
    public UserProfile User { get; set; }

    public string NotificationCategory { get; set; } = string.Empty;

    public bool InAppEnabled { get; set; } = true;

    public bool EmailEnabled { get; set; } = true;

    public Organization Organization { get; set; }
}
