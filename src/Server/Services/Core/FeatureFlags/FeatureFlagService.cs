using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.LogScope;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DevInstance.DevCoreApp.Server.Services.Core.FeatureFlags;

[BlazorService]
[BlazorServiceMock]
public class FeatureFlagService : IFeatureFlagService
{
    private const string CachePrefix = "featureflag:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IQueryRepositoryFactory? _repositoryFactory;
    private readonly IOperationContext? _operationContext;
    private readonly IMemoryCache? _cache;
    private readonly IScopeLog _log;

    public FeatureFlagService(IScopeManager logManager,
                              IQueryRepositoryFactory? repositoryFactory = null,
                              IOperationContext? operationContext = null,
                              IMemoryCache? cache = null)
    {
        _log = logManager.CreateLogger(this);
        _repositoryFactory = repositoryFactory;
        _operationContext = operationContext;
        _cache = cache;
    }

    public async Task<bool> IsEnabledAsync(string name)
    {
        using var l = _log.TraceScope();

        // In mock mode (no repository), all flags are disabled
        if (_repositoryFactory == null || _cache == null)
            return false;

        // One unit of work per call (UnitOfWork.md): flag checks run from many places in a request,
        // possibly concurrently, so they must not share the request's scoped context. A context
        // costs nothing until its first query, so a cache hit still opens no connection.
        await using var repo = _repositoryFactory.Create();

        var cacheKey = $"{CachePrefix}{name}";

        if (!_cache.TryGetValue(cacheKey, out List<FeatureFlag>? flags))
        {
            flags = await repo.GetFeatureFlagQuery(null!)
                .ByNameForEvaluation(name)
                .Select()
                .ToListAsync();

            _cache.Set(cacheKey, flags, CacheDuration);
        }

        if (flags == null || flags.Count == 0)
            return false;

        var currentUserId = _operationContext?.UserId;
        var currentOrgId = _operationContext?.PrimaryOrganizationId;
        string? currentUserPublicId = null;

        // 1. User allowlist check
        if (currentUserId.HasValue)
        {
            currentUserPublicId = await repo.GetUserProfilesQuery(null!)
                .Select()
                .Where(u => u.Id == currentUserId.Value)
                .Select(u => u.PublicId)
                .FirstOrDefaultAsync();

            var userId = currentUserId.Value.ToString();
            foreach (var flag in flags)
            {
                if (flag.AllowedUsers != null &&
                    (flag.AllowedUsers.Contains(userId) ||
                     (!string.IsNullOrWhiteSpace(currentUserPublicId) && flag.AllowedUsers.Contains(currentUserPublicId))))
                {
                    return true;
                }
            }
        }

        // 2. Org-specific flag
        if (currentOrgId.HasValue)
        {
            var orgFlag = flags.FirstOrDefault(f => f.OrganizationId == currentOrgId.Value);
            if (orgFlag != null)
                return orgFlag.IsEnabled;
        }

        // 3. Global flag (OrganizationId == null)
        var globalFlag = flags.FirstOrDefault(f => f.OrganizationId == null);
        if (globalFlag == null)
            return false;

        if (!globalFlag.IsEnabled)
            return false;

        if (globalFlag.RolloutPercentage == null || globalFlag.RolloutPercentage >= 100)
            return globalFlag.IsEnabled;

        if (globalFlag.RolloutPercentage <= 0)
            return false;

        // 4. Percentage-based rollout (consistent per-user)
        if (!currentUserId.HasValue)
            return false;

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{currentUserId.Value}{name}"));
        var bucket = Math.Abs(BitConverter.ToInt32(hash, 0)) % 100;
        return bucket < globalFlag.RolloutPercentage.Value;
    }

    public void InvalidateCache(string name)
    {
        _cache?.Remove($"{CachePrefix}{name}");
    }
}
