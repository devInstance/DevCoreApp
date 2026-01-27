using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.WebService.Authentication;
using DevInstance.DevCoreApp.Server.WebService.Tools;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;
using DevInstance.WebServiceToolkit.Database.Queries.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DevInstance.DevCoreApp.Server.WebService.Services;

[AppService]
public class UserProfileService : BaseService
{
    public UserManager<ApplicationUser> UserManager { get; }

    public UserProfileService(IScopeManager logManager,
                              ITimeProvider timeProvider,
                              IQueryRepository query,
                              IAuthorizationContext authorizationContext,
                              UserManager<ApplicationUser> userManager)
        : base(logManager, timeProvider, query, authorizationContext)
    {
        UserManager = userManager;
    }

    public ServiceActionResult<UserProfileItem> GetCurrentUser()
    {
        return ServiceActionResult<UserProfileItem>.OK(AuthorizationContext.CurrentProfile.ToView());
    }

    public async Task<ServiceActionResult<UserProfileItem>> UpdateCurrentUserAsync(UserProfileItem newProfile)
    {
        var profile = AuthorizationContext.CurrentProfile;
        profile.ToRecord(newProfile);
        await Repository.GetUserProfilesQuery(AuthorizationContext.CurrentProfile).UpdateAsync(profile);

        return ServiceActionResult<UserProfileItem>.OK(profile.ToView());
    }

    public async Task<ServiceActionResult<ModelList<UserProfileItem>>> GetAllUsersAsync(int? top, int? page)
    {
        var profilesQuery = Repository.GetUserProfilesQuery(AuthorizationContext.CurrentProfile);

        var totalCount = await profilesQuery.Clone().Select().CountAsync();
        var userProfiles = await profilesQuery.Paginate(top, page).Select().ToListAsync();

        var users = new List<UserProfileItem>();

        foreach (var profile in userProfiles)
        {
            var appUser = await UserManager.FindByIdAsync(profile.ApplicationUserId.ToString());

            if (appUser != null)
            {
                var roles = await UserManager.GetRolesAsync(appUser);
                var newUserViewModel = profile.ToView(appUser, roles);

                users.Add(newUserViewModel);
            }
        }

        var modelList = ModelListResult.CreateList(users.ToArray(), totalCount, top, page);

        return ServiceActionResult<ModelList<UserProfileItem>>.OK(modelList);
    }
}
