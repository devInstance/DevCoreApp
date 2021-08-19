using DevInstance.SampleWebApp.Server.Database.Core.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DevInstance.SampleWebApp.Server.WebService.Indentity
{
    /// <summary>
    /// This interface abstracts actual UserManager for services
    /// and eforces control over interface between framework and service.
    /// Also, it simplifies mocking user manager in unit tests.
    /// </summary>
    public interface IApplicationUserManager
    {
        string GetUserId(ClaimsPrincipal principal);

        Task<ApplicationUser> FindByIdAsync(string userId);

        Task<ApplicationUser> FindByEmailAsync(string email);

        Task<ApplicationUser> FindByNameAsync(string userName);

        Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword);

        Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword);

        Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user);

        Task<IdentityResult> DeleteAsync(ApplicationUser user);

        Task<IdentityResult> CreateAsync(ApplicationUser user, string password);

        Task<IdentityResult> UpdateAsync(ApplicationUser user);

    }
}
