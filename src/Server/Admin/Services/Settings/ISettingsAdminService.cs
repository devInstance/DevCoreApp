using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Settings;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Settings;

public interface ISettingsAdminService
{
    /// <summary>
    /// Gets all settings at the specified scope.
    /// scope: "System" or "Organization".
    /// organizationId: required when scope is "Organization".
    /// </summary>
    Task<ServiceActionResult<List<SettingItem>>> GetAllByScopeAsync(
        string scope, string? organizationId = null, string? search = null);

    /// <summary>
    /// Updates a setting value by its internal Id (Guid as string).
    /// </summary>
    Task<ServiceActionResult<SettingItem>> UpdateSettingAsync(string id, string newValue);

    /// <summary>
    /// Creates a new setting at the specified scope.
    /// </summary>
    Task<ServiceActionResult<SettingItem>> CreateSettingAsync(SettingItem item);

    /// <summary>
    /// Deletes a setting by its internal Id.
    /// </summary>
    Task<ServiceActionResult<bool>> DeleteSettingAsync(string id);
}
