using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Settings;

/// <summary>
/// Application settings service with scoped resolution.
///
/// Current runtime resolution order: User → Organization → System.
/// Tenant-level records exist in the data model for future expansion, but
/// tenant context resolution is not currently wired into the runtime path.
///
/// Results are cached in memory and invalidated on write.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets a setting value using four-tier resolution for the current user context.
    /// Returns the default value of T if no setting exists at any tier.
    /// </summary>
    Task<T?> GetAsync<T>(string category, string key);

    /// <summary>
    /// Sets a setting value at the most specific scope available for the current context.
    /// If userId is resolved, sets at user scope; otherwise falls back to org then system.
    /// To set at a specific scope, use the overload with explicit scope parameters.
    /// </summary>
    Task SetAsync<T>(string category, string key, T value);

    /// <summary>
    /// Sets a setting value at a specific scope.
    /// Pass all nulls for system scope, tenantId only for tenant scope, etc.
    /// </summary>
    Task SetAsync<T>(string category, string key, T value, Guid? tenantId, Guid? organizationId, Guid? userId);

    /// <summary>
    /// Gets all resolved settings for a category, applying four-tier resolution per key.
    /// Returns a dictionary of key → deserialized value.
    /// </summary>
    Task<Dictionary<string, object?>> GetAllForCategoryAsync(string category);
}
