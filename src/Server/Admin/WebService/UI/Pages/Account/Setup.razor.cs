using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using DevInstance.DevCoreApp.Server.Admin.WebService.Identity;
using DevInstance.DevCoreApp.Server.Admin.Services;
using DevInstance.DevCoreApp.Shared.Model.Account;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.Account;

public partial class Setup
{
    [Inject]
    private AccountService AccountService { get; set; } = default!;

    [Inject]
    private IdentityRedirectManager RedirectManager { get; set; } = default!;

    private string? errorMessage;

    [SupplyParameterFromForm]
    private SetupOwnerParameters Input { get; set; } = default!;

    private string? Message => errorMessage;

    protected override async Task OnInitializedAsync()
    {
        Input ??= new();

        // Security check: redirect if users already exist
        if (await AccountService.HasUsersAsync())
        {
            RedirectManager.RedirectTo("/");
        }
    }

    public async Task RegisterOwner(EditContext editContext)
    {
        var result = await AccountService.SetupOwnerAsync(Input);

        if (result.UsersAlreadyExist)
        {
            RedirectManager.RedirectTo("/");
            return;
        }

        if (!result.Succeeded)
        {
            errorMessage = result.ErrorMessage;
            return;
        }

        RedirectManager.RedirectTo("/");
    }
}
