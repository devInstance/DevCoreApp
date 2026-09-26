using DevInstance.BlazorToolkit.Http;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Client.Services.Core.Time;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.LogScope;
using DevInstance.DevCoreApp.Shared.Model.Core.FeatureFlags;

namespace DevInstance.DevCoreApp.Client.Services.Core.FeatureFlags;

[BlazorService]
public class FeatureFlagAdminService : ApiServiceBase, IFeatureFlagAdminService
{
    public FeatureFlagAdminService(IHttpApiContextFactory apiFactory, IScopeManager logManager) : base(apiFactory, logManager) { }

    public Task<ServiceActionResult<PagedList<FeatureFlagItem>>> GetFlagsAsync(int top, int page, string[]? sortBy = null, string? search = null) =>
        CallAsync(() => Api<FeatureFlagItem>("api/feature-flags").Get()
            .Query(new ListQuery { Top = top, Page = page, SortBy = sortBy!, Search = search! })
            .ExecuteAsync<PagedList<FeatureFlagItem>>());

    public Task<ServiceActionResult<FeatureFlagItem>> GetFlagAsync(string id) =>
        CallAsync(() => Api<FeatureFlagItem>($"api/feature-flags/{Segment(id)}").Get().ExecuteAsync());

    public Task<ServiceActionResult<FeatureFlagItem>> CreateFlagAsync(FeatureFlagItem item) =>
        CallAsync(() => Api<FeatureFlagItem>("api/feature-flags").Post(item).ExecuteAsync());

    public Task<ServiceActionResult<FeatureFlagItem>> UpdateFlagAsync(string id, FeatureFlagItem item) =>
        CallAsync(() => Api<FeatureFlagItem>($"api/feature-flags/{Segment(id)}").Put(item).ExecuteAsync());

    public Task<ServiceActionResult<bool>> DeleteFlagAsync(string id) =>
        CallAsync(() => Api<bool>($"api/feature-flags/{Segment(id)}").Delete().ExecuteAsync());
}
