using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.EmailProcessor.Smtp;

public class SmtpEmailProvider : IEmailProvider
{
    public SmtpEmailProvider(EmailConfiguration configuration)
    {
        Configuration = configuration;
    }

    public EmailConfiguration Configuration { get; }

    public async Task<EmailSendResult> SendAsync(IEmailMessage message, CancellationToken ct = default)
    {
        using var mailMessage = new MailMessage();
        mailMessage.From = new MailAddress(message.From.Address, message.From.Name);
        foreach (var to in message.To)
        {
            mailMessage.To.Add(new MailAddress(to.Address, to.Name));
        }
        mailMessage.Subject = message.Subject;
        mailMessage.Body = message.Content;
        mailMessage.IsBodyHtml = message.IsHtml;

        using var client = new SmtpClient(Configuration.SmtpServer, Configuration.Port);
        client.Credentials = new NetworkCredential(Configuration.UserName, Configuration.Password);
        client.EnableSsl = true;

        await client.SendMailAsync(mailMessage, ct);

        return new EmailSendResult
        {
            Success = true
        };
    }
}
