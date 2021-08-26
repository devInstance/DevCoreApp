using DevInstance.LogScope;
using DevInstance.SampleWebApp.Server.Database.Core.Data;
using DevInstance.SampleWebApp.Server.Database.Core.Models;
using DevInstance.SampleWebApp.Server.EmailProcessor;
using DevInstance.SampleWebApp.Server.EmailProcessor.Templates;
using DevInstance.SampleWebApp.Server.Exceptions;
using DevInstance.SampleWebApp.Server.WebService.Indentity;
using DevInstance.SampleWebApp.Server.WebService.Tools;
using DevInstance.SampleWebApp.Shared.Model;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace DevInstance.SampleWebApp.Server.Services
{
    [AppService]
    public class AuthorizationService : BaseService
    {
        private readonly IApplicationSignManager signInManager;
        protected IApplicationUserManager UserManager { get; }
        protected IEmailSender EmailSender { get; }

        public AuthorizationService(IScopeManager logManager,
                                    IQueryRepository query,
                                    IApplicationUserManager um,
                                    IApplicationSignManager sm,
                                    IAuthorizationContext authorizationContext,
                                    IEmailSender emailSender)
            : base(logManager, query, authorizationContext)
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
            var user = await UserManager.FindByEmailAsync(forgotParameters.Email);
            if (user == null)
            {
                return true;
            }

            var token = await UserManager.GeneratePasswordResetTokenAsync(user);

            var uriBuilder = new UriBuilder();
            uriBuilder.Scheme = scheme;
            uriBuilder.Host = host;
            if(port.HasValue)
            {
                uriBuilder.Port = port.Value;
            }
            uriBuilder.Path = "authentication/reset-password";
            uriBuilder.Query = $"email={user.Email}&token={HttpUtility.UrlEncode(token)}";

            await EmailSender.SendAsync(
                    TemplateFactory.CreateResetPasswordMessage(
                        new EmailAddress {
                            Name = user.UserName,
                            Address= user.Email
                        }, uriBuilder.Uri.AbsoluteUri)
                    );

            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordModel parameters)
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

