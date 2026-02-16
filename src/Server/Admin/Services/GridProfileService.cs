using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using Microsoft.EntityFrameworkCore;

namespace DevInstance.DevCoreApp.Server.Admin.Services;

[BlazorService]
[BlazorServiceMock]
public class GridProfileService : BaseService
{
    public GridProfileService(IScopeManager logManager,
                              ITimeProvider timeProvider,
                              IQueryRepository query,
                              IAuthorizationContext authorizationContext)
        : base(logManager, timeProvider, query, authorizationContext)
    {
    }

    /// <summary>
    /// Gets the grid profile for the current user and specified grid.
    /// First looks for a local profile, then falls back to global profile.
    /// Returns null if no profile exists.
    /// </summary>
    public async Task<ServiceActionResult<GridProfileItem?>> GetAsync(string gridName, string profileName = "Default")
    {
        // First, try to find user's local profile
        var profile = await Repository
            .GetGridProfilesQuery(AuthorizationContext.CurrentProfile)
            .ByUserProfileId(AuthorizationContext.CurrentProfile.Id)
            .ByGridName(gridName)
            .ByProfileName(profileName)
            .ByIsGlobal(false)
            .Select()
            .FirstOrDefaultAsync();

        // If no local profile, try to find a global profile
        if (profile == null)
        {
            profile = await Repository
                .GetGridProfilesQuery(AuthorizationContext.CurrentProfile)
                .ByGridName(gridName)
                .ByProfileName(profileName)
                .ByIsGlobal(true)
                .Select()
                .FirstOrDefaultAsync();
        }

        if (profile == null)
        {
            return ServiceActionResult<GridProfileItem?>.OK(null);
        }

        return ServiceActionResult<GridProfileItem?>.OK(profile.ToView());
    }

    /// <summary>
    /// Saves the grid profile.
    /// For local profiles: Creates or updates the profile for the current user.
    /// For global profiles: Only Owner, Admin, or Manager can create/update.
    /// </summary>
    public async Task<ServiceActionResult<GridProfileItem>> SaveAsync(GridProfileItem item)
    {
        // Authorization check for global profiles
        if (item.IsGlobal && !CanEditGlobalProfile())
        {
            throw new UnauthorizedAccessException(
                "Only Owner, Admin, or Manager can edit global profiles.");
        }

        var query = Repository.GetGridProfilesQuery(AuthorizationContext.CurrentProfile);

        // Build the query to find existing profile
        var searchQuery = query.Clone()
            .ByGridName(item.GridName)
            .ByProfileName(item.ProfileName)
            .ByIsGlobal(item.IsGlobal);

        // For local profiles, also filter by user
        if (!item.IsGlobal)
        {
            searchQuery = searchQuery.ByUserProfileId(AuthorizationContext.CurrentProfile.Id);
        }

        var existingProfile = await searchQuery.Select().FirstOrDefaultAsync();

        if (existingProfile == null)
        {
            // Create new profile
            var newProfile = query.CreateNew();
            newProfile.UserProfileId = AuthorizationContext.CurrentProfile.Id;
            newProfile.ToRecord(item);

            await query.AddAsync(newProfile);

            return ServiceActionResult<GridProfileItem>.OK(newProfile.ToView());
        }
        else
        {
            // Update existing profile
            existingProfile.ToRecord(item);
            await query.UpdateAsync(existingProfile);

            return ServiceActionResult<GridProfileItem>.OK(existingProfile.ToView());
        }
    }

    /// <summary>
    /// Checks if the current user can edit global profiles.
    /// </summary>
    private bool CanEditGlobalProfile()
    {
        return AuthorizationContext.User.IsInRole(ApplicationRoles.Owner) ||
               AuthorizationContext.User.IsInRole(ApplicationRoles.Admin) ||
               AuthorizationContext.User.IsInRole(ApplicationRoles.Manager);
    }
}
