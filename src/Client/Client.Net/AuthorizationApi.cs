using DevInstance.DevCoreApp.Client.Net.Api;
using DevInstance.DevCoreApp.Shared.Model;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Client.Net;

public class AuthorizationApi : ApiBase, IAuthorizationApi
{
    private const string Controller = "api/user/account/";

    public AuthorizationApi(HttpClient http) : base (http)
    {
    }

    public async Task RegisterAsync(RegisterParameters registerParameters)
    {
        var result = await httpClient.PostAsJsonAsync(Controller + "register", registerParameters);
        result.EnsureSuccessStatusCode();
    }

    public async Task LoginAsync(LoginParameters loginParameters)
    {
        var result = await httpClient.PostAsJsonAsync(Controller + "signin", loginParameters);
        result.EnsureSuccessStatusCode();
    }

    public async Task LogoutAsync()
    {
        var result = await httpClient.PostAsync(Controller + "signout", null);
        result.EnsureSuccessStatusCode();
    }

    public async Task<UserInfoItem> GetUserInfoAsync()
    {
        return await httpClient.GetFromJsonAsync<UserInfoItem>(Controller + "user-info");
    }

    public async Task<bool> DeleteUserAsync()
    {
        var result = await httpClient.DeleteAsync($"{Controller}");
        result.EnsureSuccessStatusCode();
        return result.Content.ReadFromJsonAsync<bool>().Result;
    }

    public async Task ChangePasswordAsync(ChangePasswordParameters chngParameters)
    {
        var result = await httpClient.PutAsJsonAsync(Controller + "change-password", chngParameters);
        result.EnsureSuccessStatusCode();
    }

    public async Task ForgotPasswordAsync(ForgotPasswordParameters forgotParameters)
    {
        var result = await httpClient.PostAsJsonAsync(Controller + "forgot-password", forgotParameters);
        result.EnsureSuccessStatusCode();
    }

    public async Task ResetPasswordAsync(ResetPasswordParameters resetPassswordParameters)
    {
        var result = await httpClient.PostAsJsonAsync(Controller + "reset-password", resetPassswordParameters);
        result.EnsureSuccessStatusCode();
    }
}
