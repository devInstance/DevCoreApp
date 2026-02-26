using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.LogScope;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Settings;

[BlazorService]
[BlazorServiceMock]
public class SettingsService : ISettingsService
{
    private const string CachePrefix = "settings:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly ApplicationDbContext _dbContext;
    private readonly IQueryRepository _repository;
    private readonly IOperationContext _operationContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMemoryCache _cache;
    private readonly IScopeLog _log;

    public SettingsService(
        IScopeManager logManager,
        ApplicationDbContext dbContext,
        IQueryRepository repository,
        IOperationContext operationContext,
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager,
        IMemoryCache cache)
    {
        _log = logManager.CreateLogger(this);
        _dbContext = dbContext;
        _repository = repository;
        _operationContext = operationContext;
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string category, string key)
    {
        using var l = _log.TraceScope();

        var context = ResolveContext();
        var cacheKey = BuildCacheKey(category, key, context);

        if (_cache.TryGetValue(cacheKey, out object? cached))
        {
            return cached is T typed ? typed : default;
        }

        var setting = await ResolveSettingAsync(category, key, context);

        if (setting == null)
        {
            _cache.Set(cacheKey, (object?)null, CacheDuration);
            return default;
        }

        var value = DeserializeValue<T>(setting.Value, setting.ValueType);
        _cache.Set(cacheKey, value, CacheDuration);
        return value;
    }

    public async Task SetAsync<T>(string category, string key, T value)
    {
        var context = ResolveContext();
        await SetAsync(category, key, value, context.TenantId, context.OrganizationId, context.UserId);
    }

    public async Task SetAsync<T>(string category, string key, T value, Guid? tenantId, Guid? organizationId, Guid? userId)
    {
        using var l = _log.TraceScope();

        var serializedValue = SerializeValue(value);
        var valueType = InferValueType<T>();

        var existing = await _dbContext.Settings
            .FirstOrDefaultAsync(s =>
                s.Category == category &&
                s.Key == key &&
                s.TenantId == tenantId &&
                s.OrganizationId == organizationId &&
                s.UserId == userId);

        if (existing != null)
        {
            existing.Value = serializedValue;
            existing.ValueType = valueType;
            _dbContext.Settings.Update(existing);
        }
        else
        {
            var settingsQuery = _repository.GetSettingsQuery(null!);
            var setting = settingsQuery.CreateNew();
            setting.TenantId = tenantId;
            setting.OrganizationId = organizationId;
            setting.UserId = userId;
            setting.Category = category;
            setting.Key = key;
            setting.Value = serializedValue;
            setting.ValueType = valueType;
            _dbContext.Settings.Add(setting);
        }

        await _dbContext.SaveChangesAsync();

        InvalidateCache(category, key);

        l.I($"Setting saved: {category}.{key}");
    }

    public async Task<Dictionary<string, object?>> GetAllForCategoryAsync(string category)
    {
        using var l = _log.TraceScope();

        var context = ResolveContext();

        // Load all settings for this category across all applicable scopes
        var allSettings = await _dbContext.Settings
            .Where(s => s.Category == category)
            .Where(s =>
                // System scope
                (s.TenantId == null && s.OrganizationId == null && s.UserId == null) ||
                // Tenant scope
                (context.TenantId != null && s.TenantId == context.TenantId && s.OrganizationId == null && s.UserId == null) ||
                // Organization scope
                (context.OrganizationId != null && s.OrganizationId == context.OrganizationId && s.UserId == null) ||
                // User scope
                (context.UserId != null && s.UserId == context.UserId))
            .ToListAsync();

        // Group by key, pick the most specific tier per key
        var result = new Dictionary<string, object?>();

        var grouped = allSettings.GroupBy(s => s.Key);
        foreach (var group in grouped)
        {
            var resolved = PickMostSpecific(group, context);
            if (resolved != null)
            {
                result[group.Key] = DeserializeValue<object>(resolved.Value, resolved.ValueType);
            }
        }

        return result;
    }

    /// <summary>
    /// Resolves the current user/org/tenant context from the HTTP request.
    /// </summary>
    private SettingsContext ResolveContext()
    {
        Guid? applicationUserId = null;
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userIdString = _userManager.GetUserId(httpContext.User);
            if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var parsed))
            {
                applicationUserId = parsed;
            }
        }

        return new SettingsContext
        {
            UserId = applicationUserId,
            OrganizationId = _operationContext?.PrimaryOrganizationId,
            TenantId = null, // Tenant resolution not yet implemented
        };
    }

    /// <summary>
    /// Queries the database for a single setting, trying tiers in order:
    /// User → Organization → Tenant → System. Returns the first match.
    /// </summary>
    private async Task<Setting?> ResolveSettingAsync(string category, string key, SettingsContext context)
    {
        var candidates = await _dbContext.Settings
            .Where(s => s.Category == category && s.Key == key)
            .Where(s =>
                (s.TenantId == null && s.OrganizationId == null && s.UserId == null) ||
                (context.TenantId != null && s.TenantId == context.TenantId && s.OrganizationId == null && s.UserId == null) ||
                (context.OrganizationId != null && s.OrganizationId == context.OrganizationId && s.UserId == null) ||
                (context.UserId != null && s.UserId == context.UserId))
            .ToListAsync();

        return PickMostSpecific(candidates, context);
    }

    /// <summary>
    /// From a set of candidate settings for the same key, pick the most specific tier.
    /// Priority: User (1) → Organization (2) → Tenant (3) → System (4).
    /// </summary>
    private static Setting? PickMostSpecific(IEnumerable<Setting> candidates, SettingsContext context)
    {
        Setting? userLevel = null;
        Setting? orgLevel = null;
        Setting? tenantLevel = null;
        Setting? systemLevel = null;

        foreach (var s in candidates)
        {
            if (s.UserId != null && context.UserId != null && s.UserId == context.UserId)
                userLevel = s;
            else if (s.OrganizationId != null && context.OrganizationId != null && s.OrganizationId == context.OrganizationId)
                orgLevel = s;
            else if (s.TenantId != null && context.TenantId != null && s.TenantId == context.TenantId)
                tenantLevel = s;
            else if (s.TenantId == null && s.OrganizationId == null && s.UserId == null)
                systemLevel = s;
        }

        return userLevel ?? orgLevel ?? tenantLevel ?? systemLevel;
    }

    private static T? DeserializeValue<T>(string json, string valueType)
    {
        if (string.IsNullOrEmpty(json))
            return default;

        return valueType switch
        {
            "string" => (T)(object)json.Trim('"'),
            "int" when typeof(T) == typeof(int) || typeof(T) == typeof(object) =>
                int.TryParse(json, out var i) ? (T)(object)i : default,
            "bool" when typeof(T) == typeof(bool) || typeof(T) == typeof(object) =>
                bool.TryParse(json, out var b) ? (T)(object)b : default,
            _ => JsonSerializer.Deserialize<T>(json),
        };
    }

    private static string SerializeValue<T>(T value)
    {
        if (value is string s)
            return JsonSerializer.Serialize(s);

        return JsonSerializer.Serialize(value);
    }

    private static string InferValueType<T>()
    {
        var type = typeof(T);
        if (type == typeof(string)) return "string";
        if (type == typeof(int)) return "int";
        if (type == typeof(bool)) return "bool";
        return "json";
    }

    private static string BuildCacheKey(string category, string key, SettingsContext context)
    {
        return $"{CachePrefix}{category}:{key}:u={context.UserId}:o={context.OrganizationId}:t={context.TenantId}";
    }

    private void InvalidateCache(string category, string key)
    {
        // IMemoryCache doesn't support prefix-based eviction. Remove entries for all
        // possible scope combinations by evicting the token that covers this (category, key).
        // For a production system, consider IDistributedCache or a cache-aside pattern.
        // Here we use a simple approach: evict by known context.
        var context = ResolveContext();
        _cache.Remove(BuildCacheKey(category, key, context));

        // Also evict system-level cache for this key (covers anonymous/background reads)
        _cache.Remove($"{CachePrefix}{category}:{key}:u=:o=:t=");
    }

    private struct SettingsContext
    {
        public Guid? UserId;
        public Guid? OrganizationId;
        public Guid? TenantId;
    }
}
