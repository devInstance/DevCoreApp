using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.FeatureFlags;

namespace DevInstance.DevCoreApp.Client.Services.Core.FeatureFlags;

/// <summary>Feature flag administration (<c>api/feature-flags</c>). Same shape as the server service.</summary>
public interface IFeatureFlagAdminService
{
    Task<ServiceActionResult<PagedList<FeatureFlagItem>>> GetFlagsAsync(int top, int page, string[]? sortBy = null, string? search = null);
    Task<ServiceActionResult<FeatureFlagItem>> GetFlagAsync(string id);
    Task<ServiceActionResult<FeatureFlagItem>> CreateFlagAsync(FeatureFlagItem item);
    Task<ServiceActionResult<FeatureFlagItem>> UpdateFlagAsync(string id, FeatureFlagItem item);
    Task<ServiceActionResult<bool>> DeleteFlagAsync(string id);
}
