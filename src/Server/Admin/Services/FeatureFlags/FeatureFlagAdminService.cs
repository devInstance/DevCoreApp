using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Shared.Model.FeatureFlags;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;
using DevInstance.WebServiceToolkit.Database.Queries.Extensions;
using DevInstance.WebServiceToolkit.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.FeatureFlags;

[BlazorService]
public class FeatureFlagAdminService : BaseService, IFeatureFlagAdminService
{
    private IScopeLog log;

    public FeatureFlagAdminService(IScopeManager logManager,
                                    ITimeProvider timeProvider,
                                    IQueryRepository query,
                                    IAuthorizationContext authorizationContext)
        : base(logManager, timeProvider, query, authorizationContext)
    {
        log = logManager.CreateLogger(this);
    }

    public async Task<ServiceActionResult<ModelList<FeatureFlagItem>>> GetFlagsAsync(
        int top, int page, string[]? sortBy = null, string? search = null)
    {
        using var l = log.TraceScope();

        var query = Repository.GetFeatureFlagQuery(AuthorizationContext.CurrentProfile);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Search(search);
        }

        var sortField = sortBy?.FirstOrDefault()?.TrimStart('-');
        var isAsc = sortBy?.FirstOrDefault()?.StartsWith("-") != true;

        query = !string.IsNullOrEmpty(sortField)
            ? query.SortBy(sortField, isAsc)
            : query.SortBy("name", true);

        var totalCount = await query.Clone().Select().CountAsync();
        var flags = await query.Paginate(top, page).Select()
            .Include(ff => ff.Organization)
            .ToListAsync();

        var items = flags.Select(ff => ff.ToView()).ToArray();

        var modelList = ModelListResult.CreateList(items, totalCount, top, page, sortBy, search);
        return ServiceActionResult<ModelList<FeatureFlagItem>>.OK(modelList);
    }

    public async Task<ServiceActionResult<FeatureFlagItem>> GetFlagAsync(string id)
    {
        using var l = log.TraceScope();

        var flag = await Repository.GetFeatureFlagQuery(AuthorizationContext.CurrentProfile)
            .ByPublicId(id)
            .Select()
            .Include(ff => ff.Organization)
            .FirstOrDefaultAsync();

        if (flag == null)
        {
            throw new RecordNotFoundException("Feature flag not found.");
        }

        return ServiceActionResult<FeatureFlagItem>.OK(flag.ToView());
    }

    public async Task<ServiceActionResult<FeatureFlagItem>> CreateFlagAsync(FeatureFlagItem item)
    {
        using var l = log.TraceScope();

        var query = Repository.GetFeatureFlagQuery(AuthorizationContext.CurrentProfile);
        var flag = query.CreateNew();
        flag.ToRecord(item);

        await query.AddAsync(flag);

        l.I($"Feature flag created: {flag.Name}");

        return ServiceActionResult<FeatureFlagItem>.OK(flag.ToView());
    }

    public async Task<ServiceActionResult<FeatureFlagItem>> UpdateFlagAsync(string id, FeatureFlagItem item)
    {
        using var l = log.TraceScope();

        var query = Repository.GetFeatureFlagQuery(AuthorizationContext.CurrentProfile);
        var flag = await query.ByPublicId(id).Select().FirstOrDefaultAsync();

        if (flag == null)
        {
            throw new RecordNotFoundException("Feature flag not found.");
        }

        flag.ToRecord(item);
        await query.UpdateAsync(flag);

        l.I($"Feature flag updated: {flag.Name}");

        return ServiceActionResult<FeatureFlagItem>.OK(flag.ToView());
    }

    public async Task<ServiceActionResult<bool>> DeleteFlagAsync(string id)
    {
        using var l = log.TraceScope();

        var query = Repository.GetFeatureFlagQuery(AuthorizationContext.CurrentProfile);
        var flag = await query.ByPublicId(id).Select().FirstOrDefaultAsync();

        if (flag == null)
        {
            throw new RecordNotFoundException("Feature flag not found.");
        }

        await query.RemoveAsync(flag);

        l.I($"Feature flag deleted: {flag.Name}");

        return ServiceActionResult<bool>.OK(true);
    }
}
