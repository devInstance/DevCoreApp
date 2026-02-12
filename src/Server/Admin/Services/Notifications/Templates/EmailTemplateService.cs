using DevInstance.LogScope;
using Microsoft.AspNetCore.Hosting;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Notifications.Templates;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IScopeLog log;

    public EmailTemplateService(IScopeManager logManager, IWebHostEnvironment environment)
    {
        log = logManager.CreateLogger(this);
        _environment = environment;
    }

    public async Task<EmailTemplateResult> RenderAsync(string templateName, Dictionary<string, string> placeholders)
    {
        using var l = log.TraceScope();

        var descriptor = EmailTemplateRepository.Get(templateName);

        var filePath = Path.Combine(_environment.WebRootPath, descriptor.RelativePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Email template file not found: {descriptor.RelativePath}", filePath);
        }

        var body = await File.ReadAllTextAsync(filePath);
        var subject = descriptor.SubjectTemplate;

        foreach (var kvp in placeholders)
        {
            var token = $"{{{{{kvp.Key}}}}}";
            body = body.Replace(token, kvp.Value);
            subject = subject.Replace(token, kvp.Value);
        }

        l.I($"Rendered email template '{templateName}'");

        return new EmailTemplateResult(subject, body, descriptor.IsHtml);
    }
}
