using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.FeatureFlags;

public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string name);
    void InvalidateCache(string name);
}
