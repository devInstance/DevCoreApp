using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using MailKit.Net.Smtp;
using MimeKit;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.EmailProcessor.MailKit
{
    public class MailKitEmailSender : IEmailSender
    {
        public MailKitEmailSender(EmailConfiguration configuration)
        {
            Configuration = configuration;
        }

        public EmailConfiguration Configuration { get; }

        public async Task SendAsync(IEmailMessage message)
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(message.From.Name, message.From.Address));
            foreach(var to in message.To)
            {
                mimeMessage.To.Add(new MailboxAddress(to.Name, to.Address));
            }
            mimeMessage.Subject = message.Subject;

            mimeMessage.Body = new TextPart("plain")
            {
                Text = message.Content
            };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(Configuration.SmtpServer, Configuration.Port, false);

                await client.AuthenticateAsync(Configuration.UserName, Configuration.Password);

                await client.SendAsync(FormatOptions.Default, mimeMessage);
                
                await client.DisconnectAsync(true);
            }
        }
    }
}
