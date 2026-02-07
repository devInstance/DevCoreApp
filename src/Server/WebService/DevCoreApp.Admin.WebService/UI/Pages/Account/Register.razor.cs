using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using DevInstance.DevCoreApp.Server.Admin.WebService.Identity;
using DevInstance.DevCoreApp.Server.Admin.WebService.Services;
using DevInstance.DevCoreApp.Shared.Model.Account;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.Account;

public partial class Register
{
    [Inject]
    private AccountService AccountService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IdentityRedirectManager RedirectManager { get; set; } = default!;

    private string? errorMessage;

    [SupplyParameterFromForm]
    private RegisterParameters Input { get; set; } = new();

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    private string? Message => errorMessage;

    public async Task RegisterUser(EditContext editContext)
    {
        var confirmationLinkBase = NavigationManager.ToAbsoluteUri("account/confirm-email").AbsoluteUri;
        var result = await AccountService.RegisterAsync(Input, confirmationLinkBase);

        if (!result.Succeeded)
        {
            errorMessage = result.ErrorMessage;
            return;
        }

        if (result.RequiresEmailConfirmation)
        {
            RedirectManager.RedirectToWithStatus(
                "Account/Login",
                "Registration successful! Please check your email to confirm your account.",
                HttpContext);
        }
        else
        {
            RedirectManager.RedirectTo(ReturnUrl);
        }
    }
}
