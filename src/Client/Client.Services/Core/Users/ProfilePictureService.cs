using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DevInstance.BlazorToolkit.Http;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Shared.Model.Core;
using DevInstance.LogScope;

namespace DevInstance.DevCoreApp.Client.Services.Core.Users;

/// <summary>
/// Binary endpoints go through the named <see cref="HttpClient"/> directly: BlazorToolkit's
/// <c>IApiContext</c> is JSON-only. The bearer token still comes from the client's handler.
/// </summary>
[BlazorService]
public class ProfilePictureService : ApiServiceBase, IProfilePictureService
{
    private readonly IHttpClientFactory httpFactory;

    public ProfilePictureService(IHttpApiContextFactory apiFactory, IHttpClientFactory httpFactory, IScopeManager logManager)
        : base(apiFactory, logManager)
    {
        this.httpFactory = httpFactory;
    }

    private HttpClient Http => httpFactory.CreateClient(ApiClient.HttpClientName);

    private static string PicturePath(string userId) => $"api/users/{Uri.EscapeDataString(userId)}/profile-picture";

    public Task<ServiceActionResult<string?>> GetDataUrlAsync(string userId, bool thumbnail = false) =>
        CallAsync<string?>(async () =>
        {
            using var response = await Http.GetAsync(PicturePath(userId) + (thumbnail ? "/thumbnail" : ""));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return (string?)null;
            }
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
        });

    public Task<ServiceActionResult<UserProfileItem>> UploadAsync(string userId, Stream image, string fileName, string contentType) =>
        CallAsync(async () =>
        {
            using var content = new MultipartFormDataContent();
            var file = new StreamContent(image);
            file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(file, "file", fileName);

            using var response = await Http.PostAsync(PicturePath(userId), content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserProfileItem>();
        });

    public Task<ServiceActionResult<bool>> DeleteAsync(string userId) =>
        CallAsync(() => Api<bool>(PicturePath(userId)).Delete().ExecuteAsync());
}
