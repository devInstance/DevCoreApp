using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.WebServiceToolkit.Database.Queries;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IEmailLogQuery : IModelQuery<EmailLog, IEmailLogQuery>,
        IQSearchable<IEmailLogQuery>,
        IQPageable<IEmailLogQuery>,
        IQSortable<IEmailLogQuery>
{
    IQueryable<EmailLog> Select();

    IEmailLogQuery ByStatus(EmailLogStatus status);
    IEmailLogQuery ByTemplateName(string templateName);
    IEmailLogQuery ByDateRange(DateTime? start, DateTime? end);

    /// <summary>Counts emails stuck in Queued status with a scheduled date earlier than the cutoff.</summary>
    Task<int> CountStuckQueuedAsync(DateTime cutoff, CancellationToken cancellationToken);
}
