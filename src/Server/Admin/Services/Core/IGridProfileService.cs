using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core.Settings;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Core;

/// <summary>
/// Per-user (and global) grid layouts: column visibility, order, page size, sort.
/// </summary>
public interface IGridProfileService
{
    /// <summary>The user's own profile for the grid, else the global one, else null.</summary>
    Task<ServiceActionResult<GridProfileItem?>> GetAsync(string gridName, string profileName = "Default");

    /// <summary>Global profiles require Owner, Admin or Manager (403 otherwise).</summary>
    Task<ServiceActionResult<GridProfileItem>> SaveAsync(GridProfileItem item);
}
