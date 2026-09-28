using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core.Settings;

namespace DevInstance.DevCoreApp.Client.Services.Core.GridProfiles;

/// <summary>Saved grid layouts (<c>api/grid-profiles</c>). Same shape as the server service.</summary>
public interface IGridProfileService
{
    /// <summary>The user's own profile for the grid, else the global one, else null.</summary>
    Task<ServiceActionResult<GridProfileItem?>> GetAsync(string gridName, string profileName = "Default");

    Task<ServiceActionResult<GridProfileItem>> SaveAsync(GridProfileItem item);
}
