using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.WebServiceToolkit.Database.Queries;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IAuditLogQuery : IModelQuery<AuditLog, IAuditLogQuery>,
        IQSearchable<IAuditLogQuery>,
        IQPageable<IAuditLogQuery>,
        IQSortable<IAuditLogQuery>
{
    IQueryable<AuditLog> Select();

    IAuditLogQuery ByTableName(string tableName);
    IAuditLogQuery ByRecordId(string recordId);
    IAuditLogQuery ByAction(AuditAction action);
    IAuditLogQuery BySource(AuditSource source);
    IAuditLogQuery ByChangedByUserId(Guid userId);
    IAuditLogQuery ByDateRange(DateTime? start, DateTime? end);
}
