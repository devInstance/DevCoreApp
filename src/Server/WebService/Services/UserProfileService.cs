using DevInstance.LogScope;
using DevInstance.SampleWebApp.Server.Database.Core.Data;
using DevInstance.SampleWebApp.Server.Database.Core.Data.Decorators;
using DevInstance.SampleWebApp.Server.Indentity;
using NoCrast.Shared.Model;

namespace DevInstance.SampleWebApp.Server.Services
{
    public class UserProfileService : BaseService
    {
        public UserProfileService(IScopeManager logManager,
                                  IQueryRepository query,
                                  IAuthorizationContext authorizationContext) 
            : base(logManager, query, authorizationContext)
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
