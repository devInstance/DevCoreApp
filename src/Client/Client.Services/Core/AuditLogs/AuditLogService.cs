using DevInstance.BlazorToolkit.Http;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Client.Services.Core.Time;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.LogScope;
using DevInstance.DevCoreApp.Shared.Model.Core.AuditLogs;

namespace DevInstance.DevCoreApp.Client.Services.Core.AuditLogs;

[BlazorService]
public class AuditLogService : ApiServiceBase, IAuditLogService
{
    private readonly ILocalTimeService time;

    public AuditLogService(IHttpApiContextFactory apiFactory, ILocalTimeService time, IScopeManager logManager)
        : base(apiFactory, logManager)
    {
        this.time = time;
    }

    public Task<ServiceActionResult<PagedList<AuditLogItem>>> GetAllAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null,
        string? search = null, int? action = null, int? source = null,
        string? tableName = null, string? recordId = null,
        DateTime? startDate = null, DateTime? endDate = null) =>
        CallAsync(() => Api<AuditLogItem>("api/audit-logs").Get()
            .Query(new AuditLogQuery
            {
                Top = top ?? 20, Page = page ?? 0, SortBy = SortBy(sortField, isAsc)!, Search = search!,
                Action = action, Source = source, TableName = tableName!, RecordId = recordId!,
                StartDate = time.ToUtc(startDate), EndDate = time.ToUtc(endDate)
            })
            .ExecuteAsync<PagedList<AuditLogItem>>());
}
