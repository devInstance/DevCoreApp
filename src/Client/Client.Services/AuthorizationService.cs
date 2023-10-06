using DevInstance.DevCoreApp.Client.Net.Api;
using DevInstance.DevCoreApp.Client.Services.Api;
using DevInstance.DevCoreApp.Shared.Model;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Client.Services;

public class AuthorizationService : BaseService, IAuthorizationService
{
    protected IAuthorizationApi Api { get; }

    public UserInfoItem CurrentUser { get; private set; }

    public AuthorizationService(IAuthorizationApi api)
    {
        Api = api;
    }

    public async Task RegisterAsync(RegisterParameters registerParameters)
    {
        await Api.RegisterAsync(registerParameters);
    }

    public async Task LoginAsync(LoginParameters loginParameters)
    {
        await Api.LoginAsync(loginParameters);
    }

    public async Task ChangePasswordAsync(ChangePasswordParameters chngParameters)
    {
        await Api.ChangePasswordAsync(chngParameters);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordParameters forgotParameters)
    {
        await Api.ForgotPasswordAsync(forgotParameters);
    }

    public async Task ResetPasswordAsync(ResetPasswordParameters resetPassswordParameters)
    {
        await Api.ResetPasswordAsync(resetPassswordParameters);
    }

    public async Task LogoutAsync()
    {
        var result = Api.LogoutAsync();
        CurrentUser = null;
        await result;
    }

    public async Task<bool> DeleteAsync()
    {
        if (await Api.DeleteUserAsync())
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
