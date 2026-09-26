using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core.AuditLogs;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;

namespace DevInstance.DevCoreApp.Client.Services.Core.AuditLogs;

/// <summary>Audit log (<c>api/audit-logs</c>). Same shape as the server service.</summary>
public interface IAuditLogService
{
    /// <param name="startDate">Local wall-clock time from a date filter; converted to UTC here.</param>
    /// <param name="endDate">Local wall-clock time from a date filter; converted to UTC here.</param>
    Task<ServiceActionResult<PagedList<AuditLogItem>>> GetAllAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null,
        string? search = null, int? action = null, int? source = null,
        string? tableName = null, string? recordId = null,
        DateTime? startDate = null, DateTime? endDate = null);
}
