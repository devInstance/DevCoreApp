using DevInstance.SampleWebApp.Client.Net.Api;
using DevInstance.SampleWebApp.Shared.Model;
using System.Threading.Tasks;

namespace DevInstance.SampleWebApp.Client.Services
{
    public class AuthorizationService
    {
        protected IAuthorizationApi Api { get; }

        public UserInfoItem CurrentUser { get; private set; }

        public AuthorizationService(IAuthorizationApi api)
        {
            Api = api;
        }

        public Task RegisterAsync(RegisterParameters registerParameters)
        {
            return Api.RegisterAsync(registerParameters);
        }

        public Task LoginAsync(LoginParameters loginParameters)
        {
            return Api.LoginAsync(loginParameters);
        }

        public Task ChangePasswordAsync(ChangePasswordParameters chngParameters)
        {
            return Api.ChangePasswordAsync(chngParameters);
        }

        public Task ForgotPasswordAsync(ForgotPasswordParameters forgotParameters)
        {
            return Api.ForgotPasswordAsync(forgotParameters);
        }

        public Task ResetPasswordAsync(ResetPasswordParameters resetPassswordParameters)
        {
            return Api.ResetPasswordAsync(resetPassswordParameters);
        }

        public Task LogoutAsync()
        {
            var result = Api.LogoutAsync();
            CurrentUser = null;
            return result;
        }

        public async Task<bool> DeleteAsync()
        {
            if(await Api.DeleteUserAsync())
            {
                CurrentUser = null;
                return true;
            }
            return false;
        }

        public async Task<UserInfoItem> GetUserInfoAsync()
        {
            if (CurrentUser != null && CurrentUser.IsAuthenticated)
            {
                return CurrentUser;
            }

            CurrentUser = await Api.GetUserInfoAsync();
            return CurrentUser;
        }
    }
}
