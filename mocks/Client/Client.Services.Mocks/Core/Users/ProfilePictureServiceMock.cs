using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Users;
using DevInstance.DevCoreApp.Shared.Model.Core;

namespace DevInstance.DevCoreApp.Client.Services.Mocks.Core.Users;

/// <summary>Uploaded pictures live in memory; users without one show their initials.</summary>
[BlazorServiceMock]
public class ProfilePictureServiceMock : IProfilePictureService
{
    private readonly Dictionary<string, string> pictures = new();

    private static string Path(string userId) => $"api/users/{userId}/profile-picture";

    public Task<ServiceActionResult<string?>> GetDataUrlAsync(string userId, bool thumbnail = false) =>
        GetDataUrlFromPathAsync(Path(userId));

    public Task<ServiceActionResult<string?>> GetDataUrlFromPathAsync(string apiPath)
    {
        var userPath = apiPath.TrimStart('/').Replace("/thumbnail", "");
        return Task.FromResult(ServiceActionResult<string?>.OK(pictures.TryGetValue(userPath, out var url) ? url : null));
    }

    public async Task<ServiceActionResult<UserProfileItem>> UploadAsync(string userId, Stream image, string fileName, string contentType)
    {
        using var buffer = new MemoryStream();
        await image.CopyToAsync(buffer);
        pictures[Path(userId)] = $"data:{contentType};base64,{Convert.ToBase64String(buffer.ToArray())}";
        return ServiceActionResult<UserProfileItem>.OK(new UserProfileItem
        {
            Id = userId,
            HasProfilePicture = true,
            ProfilePictureUrl = Path(userId),
            ProfilePictureThumbnailUrl = Path(userId) + "/thumbnail"
        });
    }

    public Task<ServiceActionResult<bool>> DeleteAsync(string userId) =>
        Task.FromResult(ServiceActionResult<bool>.OK(pictures.Remove(Path(userId))));
}
