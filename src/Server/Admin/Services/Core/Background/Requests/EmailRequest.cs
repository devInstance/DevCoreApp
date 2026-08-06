using DevInstance.DevCoreApp.Server.EmailProcessor.Core;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Core.Background.Requests;

public class EmailRequest : IEmailMessage
{
    public EmailAddress From { get; set; } = new();
    public List<EmailAddress> To { get; set; } = [];
    public string Subject { get; set; } = string.Empty;
    public bool IsHtml { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? EmailLogId { get; set; }
    public string? TemplateName { get; set; }
}
