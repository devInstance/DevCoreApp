using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using DevInstance.DevCoreApp.Shared.Model.Common;
using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models.Notifications;

public class Notification : DatabaseBaseObject, IOrganizationScoped
{
    public Guid OrganizationId { get; set; }

    public Guid UserProfileId { get; set; }
    public UserProfile User { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? LinkUrl { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime CreateDate { get; set; }

    public string? GroupKey { get; set; }

    public Organization Organization { get; set; }
}
