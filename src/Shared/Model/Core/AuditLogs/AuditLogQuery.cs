using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.WebServiceToolkit.Http.Query;

namespace DevInstance.DevCoreApp.Shared.Model.Core.AuditLogs;

/// <summary>Filters for <c>GET api/audit-logs</c>.</summary>
[QueryModel]
public class AuditLogQuery : DateRangeListQuery
{
    public int? Action { get; set; }
    public int? Source { get; set; }
    public string TableName { get; set; }
    public string RecordId { get; set; }
}
