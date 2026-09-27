namespace DevInstance.DevCoreApp.Server.Services.Core.Notifications.Templates;

public interface IEmailTemplateService
{
    Task<EmailTemplateResult> RenderAsync(string templateName, Dictionary<string, string> placeholders);
}
