﻿using DevInstance.LogScope;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Server.WebService.Authentication;
using DevInstance.DevCoreApp.Server.WebService.Tools;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.DevCoreApp.Shared.Utils;

namespace DevInstance.DevCoreApp.Server.Services
{
    [AppService]
    public class UserProfileService : BaseService
    {
        public UserProfileService(IScopeManager logManager,
                                  ITimeProvider timeProvider,
                                  IQueryRepository query,
                                  IAuthorizationContext authorizationContext)
            : base(logManager, timeProvider, query, authorizationContext)
        {

        }

        public UserProfileItem Get()
        {
            return AuthorizationContext.CurrentProfile.ToView();
        }

        public UserProfileItem Update(UserProfileItem newProfile)
        {
            var profile = AuthorizationContext.CurrentProfile;
            profile.Name = newProfile.Name;
            Repository.GetUserProfilesQuery(AuthorizationContext.CurrentProfile).Update(profile);

            return profile.ToView();
        }
    }
}
