using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core.Settings;

namespace DevInstance.DevCoreApp.Client.Services.Core.Settings;

/// <summary>Runtime settings administration (<c>api/settings</c>). Same shape as the server service.</summary>
public interface ISettingsAdminService
{
    Task<ServiceActionResult<List<SettingItem>>> GetAllByScopeAsync(string scope, string? organizationId = null, string? search = null);
    Task<ServiceActionResult<SettingItem>> UpdateSettingAsync(string id, string newValue);
    Task<ServiceActionResult<SettingItem>> CreateSettingAsync(SettingItem item);
    Task<ServiceActionResult<bool>> DeleteSettingAsync(string id);
}
