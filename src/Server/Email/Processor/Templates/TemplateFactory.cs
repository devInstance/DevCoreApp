using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.EmailProcessor.Templates
{
    public static class TemplateFactory
    {
        public static IEmailMessage CreateResetPasswordMessage(EmailAddress recipent, string uri) 
        {
            return new ResetPasswordMessage(recipent, uri);
        }
    }
}
