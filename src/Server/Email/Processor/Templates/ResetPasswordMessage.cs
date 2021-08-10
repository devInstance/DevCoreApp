using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoCrast.Server.EmailProcessor.Templates
{
    public class ResetPasswordMessage : IEmailMessage
    {
        public EmailAddress From => new EmailAddress { Name = "NoCrast Password recovery", Address = "noreply@nocrast.net" };

        public List<EmailAddress> To => new List<EmailAddress>() { Recipent };

        public string Subject => "NoCrast Password recovery";

        public string Content => GenerateContent();

        public string Uri {  get; }

        public EmailAddress Recipent { get; }

        public ResetPasswordMessage(EmailAddress recipent, string uri)
        {
            Uri = uri;
            Recipent = recipent;
        }

        protected string GenerateContent()
        {
            return $"Hi {Recipent.Name}, " +
                $"You have requested to change your password. Please click on the following link to enter your new password:" +
                $"{Uri}" +
                $" Nocrast Admin";
        }
    }
}
