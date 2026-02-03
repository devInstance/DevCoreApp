using DevInstance.DevCoreApp.Server.EmailProcessor;

namespace DevInstance.DevCoreApp.Server.WebService.Background.Requests;

public class EmailRequest : IDevEmailMessage
{
    public EmailAddress From { get; set; } = new();
    public List<EmailAddress> To { get; set; } = [];
    public string Subject { get; set; } = string.Empty;
    public bool IsHtml { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? EmailLogId { get; set; }
    public string? TemplateName { get; set; }
}
