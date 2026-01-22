using DevInstance.DevCoreApp.Server.EmailProcessor;
using DevInstance.DevCoreApp.Server.WebService.Background.Requests;

namespace DevInstance.DevCoreApp.Server.WebService.Services;

public interface IEmailSenderService
{
    Task SendAsync(EmailRequest request);
}

public class EmailSenderService : IEmailSenderService
{
    private readonly IDevEmailSender _emailSender;
    private readonly ILogger<EmailSenderService> _logger;

    public EmailSenderService(IDevEmailSender emailSender, ILogger<EmailSenderService> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task SendAsync(EmailRequest request)
    {
        try
        {
            await _emailSender.SendAsync(request);
            _logger.LogInformation("Email sent successfully to {To} with subject '{Subject}'",
                string.Join(", ", request.To.Select(t => t.Address)),
                request.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject '{Subject}'",
                string.Join(", ", request.To.Select(t => t.Address)),
                request.Subject);
            throw;
        }
    }
}
