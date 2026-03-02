using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.AuditLogs;
using DevInstance.WebServiceToolkit.Common.Model;

namespace DevInstance.DevCoreApp.Server.Admin.Services.AuditLogs;

public interface IAuditLogService
{
    Task<ServiceActionResult<ModelList<AuditLogItem>>> GetAllAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null,
        string? search = null, int? action = null, int? source = null,
        string? tableName = null, string? recordId = null,
        DateTime? startDate = null, DateTime? endDate = null);
}
