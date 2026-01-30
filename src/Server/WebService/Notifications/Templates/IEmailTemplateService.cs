namespace DevInstance.DevCoreApp.Server.WebService.Notifications.Templates;

public interface IEmailTemplateService
{
    Task<EmailTemplateResult> RenderAsync(string templateName, Dictionary<string, string> placeholders);
}
