using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.WebService.Services;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.WebServiceToolkit.Common.Model;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Server.WebService.UI.Pages.Admin;

public partial class Users
{
    [Inject]
    private UserProfileService UserService { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; }

    private ModelList<UserProfileItem>? users;

    private int pageCount = 10;

    protected override async Task OnInitializedAsync()
    {
        await LoadUsers(pageCount, null, null, null, null);
    }

    public async Task OnPageChangedAsync(int page)
    {
        await LoadUsers(pageCount, page, null, null, null);
    }

    private async Task LoadUsers(int? top, int? page, string? sortBy, bool? sortDesc, string? filter)
    {
        await Host.ServiceReadAsync(async () => await UserService.GetAllUsersAsync(top, page), (result) => users = result);
    }
}
