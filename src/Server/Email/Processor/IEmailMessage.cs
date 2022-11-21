using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.EmailProcessor
{
    public interface IEmailMessage
    {
        public EmailAddress From {  get; }
        public List<EmailAddress> To { get; }
        public string Subject { get; }
        public string Content { get; }
    }
}
