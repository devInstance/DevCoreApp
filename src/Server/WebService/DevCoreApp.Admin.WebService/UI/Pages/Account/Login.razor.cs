using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using DevInstance.DevCoreApp.Server.Admin.WebService.Identity;
using DevInstance.DevCoreApp.Server.Admin.WebService.Services;
using DevInstance.DevCoreApp.Shared.Model.Account;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.Account;

public partial class Login
{
    [Inject]
    private AccountService AccountService { get; set; } = default!;

    [Inject]
    private IdentityRedirectManager RedirectManager { get; set; } = default!;

    private string? errorMessage;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private LoginParameters Input { get; set; } = new();

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (HttpMethods.IsGet(HttpContext.Request.Method))
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }
    }

    public async Task LoginUser()
    {
        var result = await AccountService.LoginAsync(Input);

        if (result.Succeeded)
        {
            RedirectManager.RedirectTo(ReturnUrl);
        }
        else
        {
            errorMessage = result.ErrorMessage;
        }
    }
}
