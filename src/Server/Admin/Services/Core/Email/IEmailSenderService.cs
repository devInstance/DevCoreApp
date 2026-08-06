using DevInstance.DevCoreApp.Server.Admin.Services.Core.Background.Requests;
using DevInstance.DevCoreApp.Server.EmailProcessor.Core;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Core.Email;

public interface IEmailSenderService
{
    Task<EmailSendResult> SendAsync(EmailRequest request);
}
