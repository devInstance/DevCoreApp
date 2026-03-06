using System;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models.Webhooks;

public class WebhookSubscription : DatabaseObject
{
    public string EventType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
    public Guid CreatedById { get; set; }

    public UserProfile CreatedBy { get; set; } = default!;
    public Organization? Organization { get; set; }
}
