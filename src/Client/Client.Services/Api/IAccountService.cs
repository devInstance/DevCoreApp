using DevInstance.DevCoreApp.Shared.Model;

namespace DevInstance.DevCoreApp.Client.Services.Api;

public interface IAccountService
{
    Task<UserProfileItem> GetAccountAsync();

    Task<UserProfileItem> UpdateAccountNameAsync(UserProfileItem item, string newName);

    Task<UserProfileItem> UpdateUserProfileAsync(UserProfileItem item);
}
