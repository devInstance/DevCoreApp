using DevInstance.DevCoreApp.Server.Services.Core.Background.Requests;
using DevInstance.DevCoreApp.Server.EmailProcessor.Core;

namespace DevInstance.DevCoreApp.Server.Services.Core.Email;

public interface IEmailSenderService
{
    Task<EmailSendResult> SendAsync(EmailRequest request);
}
