using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.EmailProcessor
{
    public interface IEmailProvider
    {
        Task<EmailSendResult> SendAsync(IEmailMessage message, CancellationToken ct = default);
    }
}
