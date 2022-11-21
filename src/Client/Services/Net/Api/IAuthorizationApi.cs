using DevInstance.DevCoreApp.Shared.Model;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Client.Net.Api
{
    public interface IAuthorizationApi
    {
        Task RegisterAsync(RegisterParameters registerParameters);

        Task LoginAsync(LoginParameters loginParameters);

        Task LogoutAsync();

        Task<UserInfoItem> GetUserInfoAsync();

        Task<bool> DeleteUserAsync();
        
        Task ChangePasswordAsync(ChangePasswordParameters chngParameters);

        Task ForgotPasswordAsync(ForgotPasswordParameters forgotParameters);

        Task ResetPasswordAsync(ResetPasswordParameters resetPassswordParameters);
    }
}
