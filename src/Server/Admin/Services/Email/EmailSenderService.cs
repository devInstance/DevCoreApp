using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.EmailProcessor;
using DevInstance.DevCoreApp.Server.Admin.Services.Background.Requests;
using DevInstance.LogScope;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Email;

[BlazorService]
public class EmailSenderService : IEmailSenderService
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
