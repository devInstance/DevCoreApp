using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core;

namespace DevInstance.DevCoreApp.Client.Services.Core.Users;

/// <summary>
/// Profile pictures (<c>api/users/{id}/profile-picture</c>). The endpoints need the bearer token,
/// which an <c>&lt;img src&gt;</c> never sends, so pictures are fetched here and handed to the page
/// as <c>data:</c> URLs.
/// </summary>
public interface IProfilePictureService
{
    /// <summary>A <c>data:</c> URL, or null when the user has no picture.</summary>
    Task<ServiceActionResult<string?>> GetDataUrlAsync(string userId, bool thumbnail = false);

    /// <summary>
    /// A <c>data:</c> URL for a picture path as the API returns it on <c>UserProfileItem</c>
    /// (<c>ProfilePictureUrl</c> / <c>ProfilePictureThumbnailUrl</c>), or null when there is none.
    /// Cached until the user's picture is uploaded or deleted through this service.
    /// </summary>
    Task<ServiceActionResult<string?>> GetDataUrlFromPathAsync(string apiPath);

    Task<ServiceActionResult<UserProfileItem>> UploadAsync(string userId, Stream image, string fileName, string contentType);

    Task<ServiceActionResult<bool>> DeleteAsync(string userId);
}
