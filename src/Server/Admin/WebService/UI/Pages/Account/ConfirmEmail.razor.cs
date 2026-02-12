using Microsoft.AspNetCore.Components;
using DevInstance.DevCoreApp.Server.Admin.WebService.Identity;
using DevInstance.DevCoreApp.Server.Admin.Services;
using DevInstance.DevCoreApp.Shared.Model.Account;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.Account;

public partial class ConfirmEmail
{
    [Inject]
    private AccountService AccountService { get; set; } = default!;

    [Inject]
    private IdentityRedirectManager RedirectManager { get; set; } = default!;

    private bool showError;
    private string? errorMessage;
    private bool emailConfirmed;
    private bool needsPassword;
    private bool passwordSet;
    private string? passwordErrorMessage;
    private string? currentUserId;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromQuery]
    private string? UserId { get; set; }

    [SupplyParameterFromQuery]
    private string? Code { get; set; }

    [SupplyParameterFromForm]
    private SetPasswordParameters Input { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        var result = await AccountService.ConfirmEmailAsync(UserId, Code);

        if (!result.Succeeded && result.ErrorMessage is not null)
        {
            showError = true;
            errorMessage = result.ErrorMessage;

            if (result.ErrorMessage == "User not found.")
            {
                HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            }
            return;
        }

        emailConfirmed = true;
        needsPassword = result.NeedsPassword;
        currentUserId = result.UserId;
    }

    private async Task SetPasswordAsync()
    {
        if (currentUserId is null)
        {
            return;
        }

        var result = await AccountService.SetPasswordAsync(currentUserId, Input);

        if (result.Succeeded)
        {
            passwordSet = true;
            needsPassword = false;
        }
        else
        {
            passwordErrorMessage = result.ErrorMessage;
        }
    }
}
