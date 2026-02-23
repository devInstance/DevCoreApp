using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Shared.Model.Common;
using DevInstance.LogScope;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Authentication;

public class OrganizationContextResolver : IOrganizationContextResolver
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IScopeLog _log;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public OrganizationContextResolver(
        ApplicationDbContext db,
        IMemoryCache cache,
        IScopeManager logManager)
    {
        _db = db;
        _cache = cache;
        _log = logManager.CreateLogger(this);
    }

    public async Task<OrganizationContextResult> ResolveAsync(Guid userId)
    {
        var cacheKey = $"OrgContext:{userId}";

        if (_cache.TryGetValue<OrganizationContextResult>(cacheKey, out var cached))
        {
            return cached!;
        }

        var assignments = await _db.UserOrganizations
            .Include(uo => uo.Organization)
            .Where(uo => uo.UserId == userId)
            .ToListAsync();

        if (assignments.Count == 0)
        {
            var empty = new OrganizationContextResult();
            _cache.Set(cacheKey, empty, CacheDuration);
            return empty;
        }

        var visibleIds = new HashSet<Guid>();
        Guid? primaryOrgId = null;

        foreach (var assignment in assignments)
        {
            if (assignment.Organization == null)
                continue;

            // Always include the directly assigned organization
            visibleIds.Add(assignment.OrganizationId);

            if (assignment.IsPrimary)
            {
                primaryOrgId = assignment.OrganizationId;
            }

            if (assignment.Scope == OrganizationAccessScope.WithChildren)
            {
                // Use materialized path to find all descendants
                var parentPath = assignment.Organization.Path;
                var pathPrefix = parentPath + "/";

                var descendantIds = await _db.Organizations
                    .Where(o => o.Path.StartsWith(pathPrefix))
                    .Select(o => o.Id)
                    .ToListAsync();

                foreach (var id in descendantIds)
                {
                    visibleIds.Add(id);
                }
            }
        }

        // If no assignment was marked IsPrimary, fall back to the first assignment
        primaryOrgId ??= assignments[0].OrganizationId;

        var result = new OrganizationContextResult
        {
            PrimaryOrganizationId = primaryOrgId,
            VisibleOrganizationIds = visibleIds
        };

        _cache.Set(cacheKey, result, CacheDuration);

        return result;
    }

    public void InvalidateCache(Guid userId)
    {
        _cache.Remove($"OrgContext:{userId}");
    }
}
