using DevInstance.BlazorToolkit.Http;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Client.Services.Core.Time;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.LogScope;
using DevInstance.DevCoreApp.Shared.Model.Core.Settings;

namespace DevInstance.DevCoreApp.Client.Services.Core.Settings;

[BlazorService]
public class SettingsAdminService : ApiServiceBase, ISettingsAdminService
{
    public SettingsAdminService(IHttpApiContextFactory apiFactory, IScopeManager logManager) : base(apiFactory, logManager) { }

    public Task<ServiceActionResult<List<SettingItem>>> GetAllByScopeAsync(string scope, string? organizationId = null, string? search = null) =>
        CallAsync(() => Api<List<SettingItem>>("api/settings").Get()
            .Query(new SettingQuery { Scope = scope, OrganizationId = organizationId!, Search = search! })
            .ExecuteAsync());

    public Task<ServiceActionResult<SettingItem>> UpdateSettingAsync(string id, string newValue) =>
        CallAsync(() => Api<SettingItem>($"api/settings/{Segment(id)}").Put<string>(newValue).ExecuteAsync());

    public Task<ServiceActionResult<SettingItem>> CreateSettingAsync(SettingItem item) =>
        CallAsync(() => Api<SettingItem>("api/settings").Post(item).ExecuteAsync());

    public Task<ServiceActionResult<bool>> DeleteSettingAsync(string id) =>
        CallAsync(() => Api<bool>($"api/settings/{Segment(id)}").Delete().ExecuteAsync());
}
