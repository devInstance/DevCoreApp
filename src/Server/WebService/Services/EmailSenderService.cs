using DevInstance.DevCoreApp.Server.EmailProcessor;
using DevInstance.DevCoreApp.Server.WebService.Background.Requests;
using DevInstance.DevCoreApp.Server.WebService.Tools;
using DevInstance.LogScope;

namespace DevInstance.DevCoreApp.Server.WebService.Services;

[AppService]
public class EmailSenderService
{
    private readonly IDevEmailSender _emailSender;
    private readonly IScopeLog log;

    public EmailSenderService(IDevEmailSender emailSender, IScopeManager logManager)
    {
        _emailSender = emailSender;
        log = logManager.CreateLogger(this);
    }

    public async Task SendAsync(EmailRequest request)
    {
        using var l = log.TraceScope();

        try
        {
            await _emailSender.SendAsync(request);
            l.I($"Email sent successfully to {string.Join(", ", request.To.Select(t => t.Address))} with subject '{request.Subject}'");
        }
        catch (Exception ex)
        {
            l.E($"Failed to send email to {string.Join(", ", request.To.Select(t => t.Address))} with subject '{request.Subject}': {ex.Message}");
            throw;
        }
    }
}
