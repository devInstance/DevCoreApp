using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.EmailProcessor;
using DevInstance.DevCoreApp.Server.WebService.Background;
using DevInstance.DevCoreApp.Server.WebService.Background.Requests;
using DevInstance.DevCoreApp.Server.WebService.Notifications.Templates;
using Microsoft.AspNetCore.Identity;

namespace DevInstance.DevCoreApp.Server.WebService.Notifications;

public class IdentityEmailSender : IEmailSender<ApplicationUser>
{
    private readonly IBackgroundWorker _backgroundWorker;
    private readonly IConfiguration _configuration;
    private readonly IEmailTemplateService _templateService;

    public IdentityEmailSender(IBackgroundWorker backgroundWorker, IConfiguration configuration, IEmailTemplateService templateService)
    {
        _backgroundWorker = backgroundWorker;
        _configuration = configuration;
        _templateService = templateService;
    }

    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        var result = await _templateService.RenderAsync(EmailTemplateName.ConfirmEmail, new Dictionary<string, string>
        {
            ["Link"] = confirmationLink
        });

        QueueEmail(email, result, EmailTemplateName.ConfirmEmail);
    }

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var result = await _templateService.RenderAsync(EmailTemplateName.PasswordResetLink, new Dictionary<string, string>
        {
            ["Link"] = resetLink
        });

        QueueEmail(email, result, EmailTemplateName.PasswordResetLink);
    }

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var result = await _templateService.RenderAsync(EmailTemplateName.PasswordResetCode, new Dictionary<string, string>
        {
            ["Code"] = resetCode
        });

        QueueEmail(email, result, EmailTemplateName.PasswordResetCode);
    }

    private void QueueEmail(string toEmail, EmailTemplateResult result, string templateName)
    {
        var fromEmail = _configuration["EmailConfiguration:FromEmail"] ?? _configuration["EmailConfiguration:UserName"] ?? "noreply@example.com";
        var fromName = _configuration["EmailConfiguration:FromName"] ?? "DevCoreApp";

        var emailRequest = new EmailRequest
        {
            From = new EmailAddress { Name = fromName, Address = fromEmail },
            To = [new EmailAddress { Name = toEmail, Address = toEmail }],
            Subject = result.Subject,
            Content = result.Content,
            IsHtml = result.IsHtml,
            TemplateName = templateName
        };

        _backgroundWorker.Submit(new BackgroundRequestItem
        {
            RequestType = BackgroundRequestType.SendEmail,
            Content = emailRequest
        });
    }
}
