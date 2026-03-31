using DevInstance.BlazorToolkit.Services;
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
public interface ISettingsActionService
{
    /// <summary>
    /// Gets a setting value using four-tier resolution for the current user context.
    /// Returns the default value of T if no setting exists at any tier.
    /// </summary>
    Task<ServiceActionResult<T?>> GetAsync<T>(string category, string key);

    /// <summary>
    /// Sets a setting value at the most specific scope available for the current context.
    /// If userId is resolved, sets at user scope; otherwise falls back to org then system.
    /// To set at a specific scope, use the overload with explicit scope parameters.
    /// </summary>
    Task<ServiceActionResult<T?>> SetAsync<T>(string category, string key, T value);
}
