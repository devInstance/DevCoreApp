using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.SampleWebApp.Server.EmailProcessor
{
    public interface IEmailSender
    {
        Task SendAsync(IEmailMessage message);
    }
}
