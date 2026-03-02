using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Model.AuditLogs;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;

public static class AuditLogDecorators
{
    public static AuditLogItem ToView(this AuditLog auditLog)
    {
        return new AuditLogItem
        {
            Id = auditLog.Id.ToString(),
            TableName = auditLog.TableName,
            RecordId = auditLog.RecordId,
            Action = auditLog.Action.ToString(),
            OldValues = auditLog.OldValues,
            NewValues = auditLog.NewValues,
            ChangedByUserId = auditLog.ChangedByUserId?.ToString(),
            ChangedAt = auditLog.ChangedAt,
            IpAddress = auditLog.IpAddress,
            CorrelationId = auditLog.CorrelationId,
            Source = auditLog.Source.ToString()
        };
    }
}
