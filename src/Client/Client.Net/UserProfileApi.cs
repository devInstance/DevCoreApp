using DevInstance.DevCoreApp.Client.Net.Api;
using DevInstance.DevCoreApp.Shared.Model;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Client.Net;

public class UserProfileApi : ApiBase, IUserProfileApi
{
    private const string Controller = "api/user/profile/";

    public UserProfileApi(HttpClient http, NavigationManager navigationManager) : base(http, navigationManager)
    {
    }

    public async Task<UserProfileItem> GetProfileAsync()
    {
        return await httpClient.GetFromJsonAsync<UserProfileItem>($"{Controller}");
    }

    public async Task<UserProfileItem> UpdateProfileAsync(UserProfileItem item)
    {
        var result = await httpClient.PutAsJsonAsync($"{Controller}", item);
        result.EnsureSuccessStatusCode();
        return result.Content.ReadFromJsonAsync<UserProfileItem>().Result;
    }
}
