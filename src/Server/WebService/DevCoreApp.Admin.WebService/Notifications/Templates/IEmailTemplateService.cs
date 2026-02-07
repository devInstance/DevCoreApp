namespace DevInstance.DevCoreApp.Server.Admin.WebService.Notifications.Templates;

public interface IEmailTemplateService
{
    Task<EmailTemplateResult> RenderAsync(string templateName, Dictionary<string, string> placeholders);
}
