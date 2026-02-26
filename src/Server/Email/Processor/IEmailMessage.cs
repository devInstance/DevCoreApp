using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Server.EmailProcessor
{
    public interface IEmailMessage
    {
        public EmailAddress From { get; }
        public List<EmailAddress> To { get; }
        public string Subject { get; }
        public bool IsHtml { get; }
        public string Content { get; }
    }
}
