using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.GridProfiles;
using DevInstance.DevCoreApp.Shared.Model.Core.Settings;

namespace DevInstance.DevCoreApp.Client.Services.Mocks.Core.GridProfiles;

/// <summary>Grid layouts kept in memory for the session.</summary>
[BlazorServiceMock]
public class GridProfileServiceMock : IGridProfileService
{
    private readonly Dictionary<string, GridProfileItem> profiles = new();

    public Task<ServiceActionResult<GridProfileItem?>> GetAsync(string gridName, string profileName = "Default") =>
        Task.FromResult(ServiceActionResult<GridProfileItem?>.OK(
            profiles.TryGetValue($"{gridName}/{profileName}", out var profile) ? profile : null));

    public Task<ServiceActionResult<GridProfileItem>> SaveAsync(GridProfileItem item)
    {
        profiles[$"{item.GridName}/{item.ProfileName}"] = item;
        return Task.FromResult(ServiceActionResult<GridProfileItem>.OK(item));
    }
}
