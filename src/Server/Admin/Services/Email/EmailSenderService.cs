using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.EmailProcessor;
using DevInstance.DevCoreApp.Server.Admin.Services.Background.Requests;
using DevInstance.LogScope;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Email;

[BlazorService]
public class EmailSenderService : IEmailSenderService
{
    private readonly IEmailProvider _emailProvider;
    private readonly IScopeLog log;

    public EmailSenderService(IEmailProvider emailProvider, IScopeManager logManager)
    {
        _emailProvider = emailProvider;
        log = logManager.CreateLogger(this);
    }

    public async Task SendAsync(EmailRequest request)
    {
        using var l = log.TraceScope();

        try
        {
            var result = await _emailProvider.SendAsync(request);
            if (result.Success)
            {
                l.I($"Email sent successfully to {string.Join(", ", request.To.Select(t => t.Address))} with subject '{request.Subject}'");
            }
            else
            {
                l.E($"Email provider returned failure for {string.Join(", ", request.To.Select(t => t.Address))} with subject '{request.Subject}': {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            l.E($"Failed to send email to {string.Join(", ", request.To.Select(t => t.Address))} with subject '{request.Subject}': {ex.Message}");
            throw;
        }
    }
}
