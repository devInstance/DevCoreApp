using DevInstance.DevCoreApp.Client.Net.Api;
using DevInstance.DevCoreApp.Shared.Model;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Client.Services.Api;

public interface IAuthorizationService
{
    UserInfoItem CurrentUser { get; }

    Task RegisterAsync(RegisterParameters registerParameters);

    Task LoginAsync(LoginParameters loginParameters);

    Task ChangePasswordAsync(ChangePasswordParameters chngParameters);

    Task ForgotPasswordAsync(ForgotPasswordParameters forgotParameters);

    Task ResetPasswordAsync(ResetPasswordParameters resetPassswordParameters);

    Task LogoutAsync();

    Task<bool> DeleteAsync();

    Task<UserInfoItem> GetUserInfoAsync();
}
