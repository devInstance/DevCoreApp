using DevInstance.SampleWebApp.Client.Net.Api;
using DevInstance.SampleWebApp.Shared.Model;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DevInstance.SampleWebApp.Client.Net
{
    public class UserProfileApi : ApiBase, IUserProfileApi
    {
        private const string Controller = "api/user/profile/";

        public UserProfileApi(HttpClient http) : base(http)
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
}
