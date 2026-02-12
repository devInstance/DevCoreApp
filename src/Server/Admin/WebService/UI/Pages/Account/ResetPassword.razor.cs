using Microsoft.AspNetCore.Components;
using DevInstance.DevCoreApp.Server.Admin.WebService.Identity;
using DevInstance.DevCoreApp.Server.Admin.Services;
using DevInstance.DevCoreApp.Shared.Model.Account;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.Account;

public partial class ResetPassword
{
    [Inject]
    private AccountService AccountService { get; set; } = default!;

    [Inject]
    private IdentityRedirectManager RedirectManager { get; set; } = default!;

    private string? errorMessage;
    private bool invalidCode;
    private bool resetSuccessful;

    [SupplyParameterFromForm]
    private ResetPasswordParameters Input { get; set; } = new();

    [SupplyParameterFromQuery]
    private string? Code { get; set; }

    private string? Message => errorMessage;

    protected override void OnInitialized()
    {
        if (Code is null)
        {
            invalidCode = true;
            return;
        }

        Input.Code = AccountService.DecodeResetCode(Code);
    }

    private async Task OnValidSubmitAsync()
    {
        var result = await AccountService.ResetPasswordAsync(Input);

        if (result.Succeeded)
        {
            resetSuccessful = true;
        }
        else
        {
            errorMessage = result.ErrorMessage;
        }
    }
}
