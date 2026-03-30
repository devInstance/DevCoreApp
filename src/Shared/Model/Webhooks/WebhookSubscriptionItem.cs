using System;
using System.ComponentModel.DataAnnotations;
using DevInstance.WebServiceToolkit.Common.Model;

namespace DevInstance.DevCoreApp.Shared.Model.Webhooks;

public class WebhookSubscriptionItem : ModelItem
{
    [Required]
    [StringLength(256, MinimumLength = 2)]
    [Display(Name = "Event Type")]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [Url]
    [StringLength(2048)]
    [Display(Name = "URL")]
    public string Url { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Organization")]
    public string? OrganizationId { get; set; }

    public string? CreatedByName { get; set; }

    public string? OrganizationName { get; set; }

    [Display(Name = "Signing Secret")]
    public string? Secret { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime UpdateDate { get; set; }
}
