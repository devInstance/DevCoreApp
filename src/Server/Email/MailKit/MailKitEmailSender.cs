using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.EmailProcessor.MailKit;

public class MailKitEmailSender : IEmailProvider
{
    public MailKitEmailSender(EmailConfiguration configuration)
    {
        Configuration = configuration;
    }

    public EmailConfiguration Configuration { get; }

    public async Task<EmailSendResult> SendAsync(IEmailMessage message, CancellationToken ct = default)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(message.From.Name, message.From.Address));
        foreach (var to in message.To)
        {
            mimeMessage.To.Add(new MailboxAddress(to.Name, to.Address));
        }
        mimeMessage.Subject = message.Subject;

        var subtype = message.IsHtml ? "html" : "plain";
        mimeMessage.Body = new TextPart(subtype)
        {
            Text = message.Content
        };

        using (var client = new SmtpClient())
        {
            await client.ConnectAsync(Configuration.SmtpServer, Configuration.Port, SecureSocketOptions.Auto, ct);

            await client.AuthenticateAsync(Configuration.UserName, Configuration.Password, ct);

            var response = await client.SendAsync(FormatOptions.Default, mimeMessage, ct);

            await client.DisconnectAsync(true, ct);

            return new EmailSendResult
            {
                Success = true,
                ProviderId = response
            };
        }
    }
}
