using DevInstance.DevCoreApp.Client.Services.Api;
using DevInstance.DevCoreApp.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Client.ClientMocks.ServicesMocks
{
    internal class AuthorizationServiceMock : IAuthorizationService
    {
        public AuthorizationServiceMock()
        {
            CurrentUser = new UserInfoItem()
            {
                UserName = "Test@test.com",
                IsAuthenticated = true,
                Id = "test892317jhdaskj",
                ExposedClaims = new Dictionary<string, string> { { "test", "test" } }
            };
        }

        public UserInfoItem CurrentUser { get; private set; }

        public async Task ChangePasswordAsync(ChangePasswordParameters chngParameters)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> DeleteAsync()
        {
            throw new NotImplementedException();
        }

        public async Task ForgotPasswordAsync(ForgotPasswordParameters forgotParameters)
        {
            throw new NotImplementedException();
        }

        public async Task<UserInfoItem> GetUserInfoAsync()
        {
            return CurrentUser;
        }

        public async Task LoginAsync(LoginParameters loginParameters)
        {
            throw new NotImplementedException();
        }

        public async Task LogoutAsync()
        {
            throw new NotImplementedException();
        }

        public async Task RegisterAsync(RegisterParameters registerParameters)
        {
            throw new NotImplementedException();
        }

        public async Task ResetPasswordAsync(ResetPasswordParameters resetPassswordParameters)
        {
            throw new NotImplementedException();
        }
    }
}
