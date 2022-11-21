using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.EmailProcessor
{
    public interface IEmailSender
    {
        Task SendAsync(IEmailMessage message);
    }
}
