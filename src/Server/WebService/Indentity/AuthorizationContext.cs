using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Security.Claims;

namespace DevInstance.DevCoreApp.Server.WebService.Indentity
{
    public class AuthorizationContext : IAuthorizationContext
    {
        protected UserManager<ApplicationUser> UserManager { get; }

        private UserProfile currentProfile;
        private IQueryRepository Query { get; }

        private IHttpContextAccessor HttpContextAccessor { get; }

        public AuthorizationContext(IHttpContextAccessor httpContextAccessor,
                                    IQueryRepository q,
                                    UserManager<ApplicationUser> userManager)
        {
            HttpContextAccessor = httpContextAccessor;
            UserManager = userManager;
            Query = q;
        }

        public UserProfile CurrentProfile
        {
            get
            {
                if (currentProfile == null)
                {
                    string userId = UserManager.GetUserId(HttpContextAccessor.HttpContext.User);
                    if (!String.IsNullOrEmpty(userId))
                    {
                        currentProfile = Query.GetUserProfilesQuery(null).ByApplicationUserId(Guid.Parse(userId)).Select().FirstOrDefault();
                    }
                }
                return currentProfile;
            }
        }

        public ClaimsPrincipal User => HttpContextAccessor.HttpContext.User;

        public void ResetCurrentProfile()
        {
            currentProfile = null;
        }

        public UserProfile FindUserProfile(ApplicationUser user)
        {
            if (user != null && user.Id != Guid.Empty)
            {
                return Query.GetUserProfilesQuery(null).ByApplicationUserId(user.Id).Select().FirstOrDefault();
            }
            return null;
        }
    }
}
