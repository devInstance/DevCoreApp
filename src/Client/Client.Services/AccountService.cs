using DevInstance.LogScope;
using DevInstance.DevCoreApp.Client.Net.Api;
using DevInstance.DevCoreApp.Shared.Model;
using System;
using System.Threading.Tasks;
using DevInstance.DevCoreApp.Client.Services.Api;

namespace DevInstance.DevCoreApp.Client.Services;

public class AccountService : BaseService, IAccountService
{
    private IUserProfileApi UserProfileApi { get; }

    public AccountService(IScopeManager logProvider,
                            IUserProfileApi api)
    {
        UserProfileApi = api;
        Log = logProvider.CreateLogger(this);
        Log.D("constructor");
    }

    public async Task<UserProfileItem> GetAccountAsync()
    {
        using (var l = Log.TraceScope())
        {
            try
            {
                var response = await UserProfileApi.GetProfileAsync();
                return response;
            }
            catch (Exception ex)
            {
                //                    NotifyNetworkError(ex);
            }
            return null;
        }
    }

    public async Task<UserProfileItem> UpdateAccountNameAsync(UserProfileItem item, string newName)
    {
        using (var l = Log.TraceScope())
        {
            try
            {
                item.Name = newName;
                var response = await UserProfileApi.UpdateProfileAsync(item);
                return response;
            }
            catch (Exception ex)
            {
                //                    NotifyNetworkError(ex);
            }
            return item;
        }
    }

    public async Task<UserProfileItem> UpdateUserProfileAsync(UserProfileItem item)
    {
        using (var l = Log.TraceScope())
        {
            try
            {
                var response = await UserProfileApi.UpdateProfileAsync(item);

                return response;
            }
            catch (Exception ex)
            {
                //                    NotifyNetworkError(ex);
            }
            return item;
        }
    }
}
