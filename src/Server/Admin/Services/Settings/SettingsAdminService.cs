using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Shared.Model.Settings;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Settings;

[BlazorService]
public class SettingsAdminService : BaseService, ISettingsAdminService
{
    private readonly ISettingsCacheInvalidator _cacheInvalidator;
    private IScopeLog log;

    public SettingsAdminService(IScopeManager logManager,
                                ITimeProvider timeProvider,
                                IQueryRepository query,
                                IAuthorizationContext authorizationContext,
                                ISettingsCacheInvalidator cacheInvalidator)
        : base(logManager, timeProvider, query, authorizationContext)
    {
        log = logManager.CreateLogger(this);
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<ServiceActionResult<List<SettingItem>>> GetAllByScopeAsync(
        string scope, string? organizationId = null, string? search = null)
    {
        using var l = log.TraceScope();

        var query = Repository.GetSettingsQuery(AuthorizationContext.CurrentProfile);

        switch (scope)
        {
            case "System":
                query = query.SystemOnly();
                break;
            case "Organization":
                if (!string.IsNullOrEmpty(organizationId) && Guid.TryParse(organizationId, out var orgGuid))
                {
                    query = query.ByOrganizationId(orgGuid).ByUserId(null);
                }
                else
                {
                    // Return all org-scoped settings (OrganizationId != null, UserId == null)
                    query = query.ByUserId(null);
                }
                break;
            case "Tenant":
            case "User":
                throw new BadRequestException($"The {scope} settings scope is not exposed in the admin UI yet.");
            default:
                throw new BadRequestException("Unsupported settings scope.");
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Search(search);
        }

        query = query.SortBy("category", true);

        var settings = await query.Select().ToListAsync();

        // Post-filter for Organization scope without specific org: only include org-scoped
        if (scope == "Organization" && string.IsNullOrEmpty(organizationId))
        {
            settings = settings.Where(s => s.OrganizationId != null).ToList();
        }

        var items = settings.Select(s =>
        {
            var item = s.ToView();
            // Resolve organization name if org-scoped
            if (s.Organization != null)
            {
                item.OrganizationName = s.Organization.Name;
            }
            return item;
        }).ToList();

        return ServiceActionResult<List<SettingItem>>.OK(items);
    }

    public async Task<ServiceActionResult<SettingItem>> UpdateSettingAsync(string id, string newValue)
    {
        using var l = log.TraceScope();

        if (!Guid.TryParse(id, out var guid))
        {
            throw new BadRequestException("Invalid setting ID.");
        }

        var query = Repository.GetSettingsQuery(AuthorizationContext.CurrentProfile);
        var setting = await query.ByPublicId(id).Select().FirstOrDefaultAsync();

        if (setting == null)
        {
            throw new RecordNotFoundException("Setting not found.");
        }

        if (!(setting.IsSensitive && newValue == string.Empty))
        {
            setting.Value = newValue;
        }
        await query.UpdateAsync(setting);
        _cacheInvalidator.Invalidate(setting.Category, setting.Key);

        l.I($"Setting updated: {setting.Category}.{setting.Key}");

        return ServiceActionResult<SettingItem>.OK(setting.ToView());
    }

    public async Task<ServiceActionResult<SettingItem>> CreateSettingAsync(SettingItem item)
    {
        using var l = log.TraceScope();

        var query = Repository.GetSettingsQuery(AuthorizationContext.CurrentProfile);
        var setting = query.CreateNew();

        setting.Category = item.Category;
        setting.Key = item.Key;
        setting.Value = item.Value;
        setting.ValueType = item.ValueType;
        setting.Description = item.Description;
        setting.IsSensitive = item.IsSensitive;

        switch (item.Scope)
        {
            case "Organization":
                if (!string.IsNullOrEmpty(item.OrganizationId) && Guid.TryParse(item.OrganizationId, out var orgGuid))
                {
                    setting.OrganizationId = orgGuid;
                }
                break;
            case "Tenant":
            case "User":
                throw new BadRequestException($"The {item.Scope} settings scope is not exposed in the admin UI yet.");
            case "System":
            default:
                // All scope FKs remain null
                break;
        }

        await query.AddAsync(setting);
        _cacheInvalidator.Invalidate(setting.Category, setting.Key);

        l.I($"Setting created: {setting.Category}.{setting.Key} at scope {item.Scope}");

        return ServiceActionResult<SettingItem>.OK(setting.ToView());
    }

    public async Task<ServiceActionResult<bool>> DeleteSettingAsync(string id)
    {
        using var l = log.TraceScope();

        if (!Guid.TryParse(id, out var guid))
        {
            throw new BadRequestException("Invalid setting ID.");
        }

        var query = Repository.GetSettingsQuery(AuthorizationContext.CurrentProfile);
        var setting = await query.ByPublicId(id).Select().FirstOrDefaultAsync();

        if (setting == null)
        {
            throw new RecordNotFoundException("Setting not found.");
        }

        var cacheCategory = setting.Category;
        var cacheKey = setting.Key;
        await query.RemoveAsync(setting);
        _cacheInvalidator.Invalidate(cacheCategory, cacheKey);

        l.I($"Setting deleted: {cacheCategory}.{cacheKey}");

        return ServiceActionResult<bool>.OK(true);
    }
}
