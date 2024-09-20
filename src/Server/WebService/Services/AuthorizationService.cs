using DevInstance.LogScope;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.EmailProcessor;
using DevInstance.DevCoreApp.Server.EmailProcessor.Templates;
using DevInstance.DevCoreApp.Server.Exceptions;
using DevInstance.DevCoreApp.Server.WebService.Authentication;
using DevInstance.DevCoreApp.Server.WebService.Tools;
using DevInstance.DevCoreApp.Shared.Model;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using DevInstance.DevCoreApp.Shared.Utils;

namespace DevInstance.DevCoreApp.Server.Services
{
    [AppService]
    public class AuthorizationService : BaseService
    {
        private readonly IApplicationSignManager signInManager;
        protected IApplicationUserManager UserManager { get; }
        protected IEmailSender EmailSender { get; }

        public AuthorizationService(IScopeManager logManager,
                                    ITimeProvider timeProvider,
                                    IQueryRepository query,
                                    IApplicationUserManager um,
                                    IApplicationSignManager sm,
                                    IAuthorizationContext authorizationContext,
                                    IEmailSender emailSender)
            : base(logManager, timeProvider, query, authorizationContext)
        {
            signInManager = sm;
            EmailSender = emailSender;
            UserManager = um;
        }

        public async Task<bool> Login(LoginParameters parameters)
        {
            AuthorizationContext.ResetCurrentProfile();

            var user = await UserManager.FindByNameAsync(parameters.UserName);
            if (user == null)
            {
                throw new UnauthorizedException("User does not exist");
            }

            var singInResult = await signInManager.SignInAsync(user, parameters.Password, parameters.RememberMe);
            if (!singInResult.Succeeded)
            {
                throw new UnauthorizedException("Invalid password");
            }

            var userProfile = AuthorizationContext.FindUserProfile(user);
            if (userProfile == null)
            {
                var q = Repository.GetUserProfilesQuery(null);
                var record = q.CreateNew();
                record.Status = UserStatus.LIVE;
                record.ApplicationUserId = user.Id;
                record.Name = parameters.UserName;
                record.Email = parameters.UserName;

                q.Add(record);
            }

            return true;
        }

        public async Task<bool> Register(RegisterParameters parameters)
        {
            var user = new ApplicationUser();
            user.UserName = parameters.UserName;
            var result = await UserManager.CreateAsync(user, parameters.Password);
            if (!result.Succeeded)
            {
                throw new BadRequestException(result.Errors.FirstOrDefault()?.Description);
            }

            return await Login(new LoginParameters
            {
                UserName = parameters.UserName,
                Password = parameters.Password
            });
        }

        public async Task<bool> Logout()
        {
            await signInManager.SignOutAsync();
            AuthorizationContext.ResetCurrentProfile();
            return true;
        }

        public UserInfoItem GetUserInfo()
        {
            return new UserInfoItem
            {
                IsAuthenticated = AuthorizationContext.User.Identity.IsAuthenticated,
                UserName = AuthorizationContext.User.Identity.Name,
                ExposedClaims = AuthorizationContext.User.Claims.ToDictionary(c => c.Type, c => c.Value)
            };
        }

        public async Task<bool> Delete()
        {
            await signInManager.SignOutAsync();

            var user = await UserManager.FindByNameAsync(AuthorizationContext.CurrentProfile.Email);
            if (user == null)
            {
                throw new UnauthorizedException("User does not exist");
            }

            //await Repository.DeleteAllDataAsync(AuthorizationContext.CurrentProfile);

            var result = await UserManager.DeleteAsync(user);
            AuthorizationContext.ResetCurrentProfile();

            return true;
        }

        public async Task<bool> ChangePassword(ChangePasswordParameters parameters)
        {
            var userId = UserManager.GetUserId(AuthorizationContext.User);
            var user = await UserManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new UnauthorizedException("User does not exist");
            }

            var result = await UserManager.ChangePasswordAsync(user, parameters.OldPassword, parameters.NewPassword);
            if (!result.Succeeded)
            {
                throw new UnauthorizedException(result.Errors.FirstOrDefault()?.Description);
            }
            return true;
        }

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordParameters forgotParameters, string scheme, string host, int? port)
        {
            // Verify if the given email address exists in the system
            var user = await UserManager.FindByEmailAsync(forgotParameters.Email);
            if (user == null)
            {
                // Pretend that email has been sent
                return true;
            }
            // Generate reset password token
            var token = await UserManager.GeneratePasswordResetTokenAsync(user);

            // Create the url for the password reset
            var uriBuilder = new UriBuilder();
            uriBuilder.Scheme = scheme;
            uriBuilder.Host = host;
            if(port.HasValue)
            {
                uriBuilder.Port = port.Value;
            }
            uriBuilder.Path = "authentication/reset-password";
            uriBuilder.Query = $"email={user.Email}&token={HttpUtility.UrlEncode(token)}";

            // Send email to the customer
            await EmailSender.SendAsync(
                    TemplateFactory.CreateResetPasswordMessage(
                        new EmailAddress {
                            Name = user.UserName,
                            Address= user.Email
                        }, uriBuilder.Uri.AbsoluteUri)
                    );

            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordParameters parameters)
        {
            using (var l = log.DebugScope())
            {
                var user = await UserManager.FindByEmailAsync(parameters.Email);
                if (user == null)
                {
                    return true;
                }

                var resetPassResult = await UserManager.ResetPasswordAsync(user, parameters.Token, parameters.Password);
                if (!resetPassResult.Succeeded)
                {
                    foreach (var error in resetPassResult.Errors)
                    {
                        //TODO: provide right error back to client
                        l.E($"code:{error.Code}, Description:{error.Description}");
                    }
                    throw new BadRequestException();
                }

                return true;
            }
        }

    }
}

