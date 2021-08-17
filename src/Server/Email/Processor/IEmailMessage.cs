using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.SampleWebApp.Server.EmailProcessor
{
    public interface IEmailMessage
    {
        public EmailAddress From {  get; }
        public List<EmailAddress> To { get; }
        public string Subject { get; }
        public string Content { get; }
    }
}
