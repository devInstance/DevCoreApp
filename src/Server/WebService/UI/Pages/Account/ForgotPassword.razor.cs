using Microsoft.AspNetCore.Components;
using DevInstance.DevCoreApp.Server.WebService.Identity;
using DevInstance.DevCoreApp.Server.WebService.Services;
using DevInstance.DevCoreApp.Shared.Model.Account;

namespace DevInstance.DevCoreApp.Server.WebService.UI.Pages.Account;

public partial class ForgotPassword
{
    [Inject]
    private AccountService AccountService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IdentityRedirectManager RedirectManager { get; set; } = default!;

    private bool submitted;

    [SupplyParameterFromForm]
    private ForgotPasswordParameters Input { get; set; } = new();

    private async Task OnValidSubmitAsync()
    {
        var resetLinkBase = NavigationManager.ToAbsoluteUri("Account/ResetPassword").AbsoluteUri;
        await AccountService.SendPasswordResetLinkAsync(Input, resetLinkBase);

        // Always show success to prevent user enumeration
        submitted = true;
    }
}
