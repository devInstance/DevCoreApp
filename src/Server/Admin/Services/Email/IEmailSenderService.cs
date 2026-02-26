using DevInstance.DevCoreApp.Server.Admin.Services.Background.Requests;
using DevInstance.DevCoreApp.Server.EmailProcessor;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Email;

public interface IEmailSenderService
{
    Task<EmailSendResult> SendAsync(EmailRequest request);
}
