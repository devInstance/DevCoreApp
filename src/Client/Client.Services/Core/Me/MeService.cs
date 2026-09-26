using DevInstance.BlazorToolkit.Http;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Shared.Model.Core;
using DevInstance.DevCoreApp.Shared.Model.Core.UserAdmin;
using DevInstance.LogScope;

namespace DevInstance.DevCoreApp.Client.Services.Core.Me;

[BlazorService]
public class MeService : ApiServiceBase, IMeService
{
    public MeService(IHttpApiContextFactory apiFactory, IScopeManager logManager) : base(apiFactory, logManager) { }

    public Task<ServiceActionResult<CurrentUserItem>> GetAsync() =>
        CallAsync(() => Api<CurrentUserItem>("api/me").Get().ExecuteAsync());

    public Task<ServiceActionResult<UserProfileItem>> UpdateProfileAsync(UserProfileItem profile) =>
        CallAsync(() => Api<UserProfileItem>("api/me/profile").Put(profile).ExecuteAsync());

    public Task<ServiceActionResult<string>> SetThemeAsync(string theme) =>
        CallAsync(() => Api<string>("api/me/theme").Put(theme).ExecuteAsync());
}
