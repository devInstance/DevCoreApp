using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Model.AuditLogs;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;
using DevInstance.WebServiceToolkit.Database.Queries.Extensions;
using Microsoft.EntityFrameworkCore;

namespace DevInstance.DevCoreApp.Server.Admin.Services.AuditLogs;

[BlazorService]
public class AuditLogService : BaseService, IAuditLogService
{
    private IScopeLog log;

    public AuditLogService(IScopeManager logManager,
                           ITimeProvider timeProvider,
                           IQueryRepository query,
                           IAuthorizationContext authorizationContext)
        : base(logManager, timeProvider, query, authorizationContext)
    {
        log = logManager.CreateLogger(this);
    }

    public async Task<ServiceActionResult<ModelList<AuditLogItem>>> GetAllAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null,
        string? search = null, int? action = null, int? source = null,
        string? tableName = null, string? recordId = null,
        DateTime? startDate = null, DateTime? endDate = null)
    {
        using var l = log.TraceScope();

        var query = Repository.GetAuditLogQuery(AuthorizationContext.CurrentProfile);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Search(search);
        }

        if (action.HasValue && Enum.IsDefined(typeof(AuditAction), action.Value))
        {
            query = query.ByAction((AuditAction)action.Value);
        }

        if (source.HasValue && Enum.IsDefined(typeof(AuditSource), source.Value))
        {
            query = query.BySource((AuditSource)source.Value);
        }

        if (!string.IsNullOrEmpty(tableName))
        {
            query = query.ByTableName(tableName);
        }

        if (!string.IsNullOrEmpty(recordId))
        {
            query = query.ByRecordId(recordId);
        }

        if (startDate.HasValue || endDate.HasValue)
        {
            query = query.ByDateRange(startDate, endDate);
        }

        query = !string.IsNullOrEmpty(sortField)
            ? query.SortBy(sortField, isAsc ?? true)
            : query.SortBy("changedat", false);

        var totalCount = await query.Clone().Select().CountAsync();
        var auditLogs = await query.Paginate(top, page).Select().ToListAsync();

        var items = auditLogs.Select(a => a.ToView()).ToArray();

        // Resolve user names for entries that have a ChangedByUserId
        var userIds = items
            .Where(i => !string.IsNullOrEmpty(i.ChangedByUserId))
            .Select(i => Guid.Parse(i.ChangedByUserId!))
            .Distinct()
            .ToList();

        if (userIds.Count > 0)
        {
            var userQuery = Repository.GetUserProfilesQuery(AuthorizationContext.CurrentProfile);
            var users = await userQuery.Select()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .ToListAsync();

            var userNameMap = users.ToDictionary(
                u => u.Id.ToString(),
                u => $"{u.FirstName} {u.LastName}".Trim());

            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.ChangedByUserId) &&
                    userNameMap.TryGetValue(item.ChangedByUserId, out var name))
                {
                    item.ChangedByUserName = name;
                }
            }
        }

        string[]? sortBy = !string.IsNullOrEmpty(sortField)
            ? new[] { (isAsc == false ? "-" : "") + sortField }
            : null;

        var modelList = ModelListResult.CreateList(items, totalCount, top, page, sortBy, search);
        return ServiceActionResult<ModelList<AuditLogItem>>.OK(modelList);
    }
}
