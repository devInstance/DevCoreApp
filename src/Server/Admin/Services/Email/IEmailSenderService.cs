using DevInstance.DevCoreApp.Server.Admin.Services.Background.Requests;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Email;

public interface IEmailSenderService
{
    Task SendAsync(EmailRequest request);
}
