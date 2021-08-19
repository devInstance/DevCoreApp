using DevInstance.SampleWebApp.Server.Database.Core.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DevInstance.SampleWebApp.Server.WebService.Indentity
{
    public class ApplicationUserManager : IApplicationUserManager
    {
        protected UserManager<ApplicationUser> UserManager { get; }

        public ApplicationUserManager(UserManager<ApplicationUser> userManager)
        {
            UserManager = userManager;
        }

        public Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword)
        {
            return UserManager.ChangePasswordAsync(user, currentPassword, newPassword);
        }

        public Task<IdentityResult> CreateAsync(ApplicationUser user, string password)
        {
            return UserManager.CreateAsync(user, password);
        }

        public Task<IdentityResult> DeleteAsync(ApplicationUser user)
        {
            return UserManager.DeleteAsync(user);
        }

        public Task<ApplicationUser> FindByEmailAsync(string email)
        {
            return UserManager.FindByEmailAsync(email);
        }

        public Task<ApplicationUser> FindByIdAsync(string userId)
        {
            return UserManager.FindByIdAsync(userId);
        }

        public Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
        {
            return UserManager.GeneratePasswordResetTokenAsync(user);
        }

        public string GetUserId(ClaimsPrincipal principal)
        {
            return UserManager.GetUserId(principal);
        }

        public Task<IdentityResult> UpdateAsync(ApplicationUser user)
        {
            return UserManager.UpdateAsync(user);
        }

        public Task<ApplicationUser> FindByNameAsync(string userName)
        {
            return UserManager.FindByNameAsync(userName);
        }

        public Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword)
        {
            return UserManager.ResetPasswordAsync(user, token, newPassword);
        }
    }
}
