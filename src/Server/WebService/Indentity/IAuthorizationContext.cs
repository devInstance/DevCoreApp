using DevInstance.SampleWebApp.Server.Database.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DevInstance.SampleWebApp.Server.WebService.Indentity
{
    public interface IAuthorizationContext
    {
        UserProfile CurrentProfile { get; }
        ClaimsPrincipal User { get; }
        void ResetCurrentProfile();

        UserProfile FindUserProfile(ApplicationUser user);
    }
}
