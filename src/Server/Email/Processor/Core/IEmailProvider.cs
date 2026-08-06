using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.EmailProcessor.Core
{
    public interface IEmailProvider
    {
        Task<EmailSendResult> SendAsync(IEmailMessage message, CancellationToken ct = default);
    }
}
