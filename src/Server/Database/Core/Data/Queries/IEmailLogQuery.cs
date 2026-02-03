using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.WebServiceToolkit.Database.Queries;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IEmailLogQuery : IModelQuery<EmailLog, IEmailLogQuery>,
        IQSearchable<IEmailLogQuery>,
        IQPageable<IEmailLogQuery>,
        IQSortable<IEmailLogQuery>
{
    IQueryable<EmailLog> Select();

    IEmailLogQuery ByStatus(EmailLogStatus status);
    IEmailLogQuery ByDateRange(DateTime? start, DateTime? end);
}
