﻿using DevInstance.SampleWebApp.Server.Database.Core.Data;
using DevInstance.SampleWebApp.Server.Database.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Security.Claims;

namespace DevInstance.SampleWebApp.Server.Indentity
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
                        currentProfile = Query.GetUserProfilesQuery(null).ByApplicationUserId(userId).Select().FirstOrDefault();
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
            if (user != null && user.Id != null)
            {
                return Query.GetUserProfilesQuery(null).ByApplicationUserId(user.Id).Select().FirstOrDefault();
            }
            return null;
        }
    }
}