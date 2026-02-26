using System;
using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.EmailProcessor.SendGrid;

public class SendGridEmailProvider : IEmailProvider
{
    public SendGridEmailProvider(string apiKey)
    {
        ApiKey = apiKey;
    }

    public string ApiKey { get; }

    public Task<EmailSendResult> SendAsync(IEmailMessage message, CancellationToken ct = default)
    {
        throw new NotImplementedException("SendGrid provider is not yet implemented.");
    }
}
