using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core.FeatureFlags;
using DevInstance.WebServiceToolkit.Common.Model;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Core.FeatureFlags;

public interface IFeatureFlagAdminService
{
    Task<ServiceActionResult<ModelList<FeatureFlagItem>>> GetFlagsAsync(
        int top, int page, string[]? sortBy = null, string? search = null);

    Task<ServiceActionResult<FeatureFlagItem>> GetFlagAsync(string id);

    Task<ServiceActionResult<FeatureFlagItem>> CreateFlagAsync(FeatureFlagItem item);

    Task<ServiceActionResult<FeatureFlagItem>> UpdateFlagAsync(string id, FeatureFlagItem item);

    Task<ServiceActionResult<bool>> DeleteFlagAsync(string id);
}
