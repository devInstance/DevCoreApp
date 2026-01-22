using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.EmailProcessor;
using DevInstance.DevCoreApp.Server.WebService.Background;
using DevInstance.DevCoreApp.Server.WebService.Background.Requests;
using Microsoft.AspNetCore.Identity;

namespace DevInstance.DevCoreApp.Server.WebService.Notifications;

public class IdentityEmailSender : IEmailSender<ApplicationUser>
{
    private readonly IBackgroundWorker _backgroundWorker;
    private readonly IConfiguration _configuration;

    public IdentityEmailSender(IBackgroundWorker backgroundWorker, IConfiguration configuration)
    {
        _backgroundWorker = backgroundWorker;
        _configuration = configuration;
    }

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        var subject = "Confirm your email";
        var content = $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.";
        return QueueEmailAsync(email, subject, content, isHtml: true);
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var subject = "Reset your password";
        var content = $"Please reset your password by <a href='{resetLink}'>clicking here</a>.";
        return QueueEmailAsync(email, subject, content, isHtml: true);
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var subject = "Reset your password";
        var content = $"Please reset your password using the following code: {resetCode}";
        return QueueEmailAsync(email, subject, content, isHtml: false);
    }

    private Task QueueEmailAsync(string toEmail, string subject, string content, bool isHtml)
    {
        var fromEmail = _configuration["EmailConfiguration:FromEmail"] ?? _configuration["EmailConfiguration:UserName"] ?? "noreply@example.com";
        var fromName = _configuration["EmailConfiguration:FromName"] ?? "DevCoreApp";

        var emailRequest = new EmailRequest
        {
            From = new EmailAddress { Name = fromName, Address = fromEmail },
            To = [new EmailAddress { Name = toEmail, Address = toEmail }],
            Subject = subject,
            Content = content,
            IsHtml = isHtml
        };

        _backgroundWorker.Submit(new BackgroundRequestItem
        {
            RequestType = BackgroundRequestType.SendEmail,
            Content = emailRequest
        });

        return Task.CompletedTask;
    }
}
