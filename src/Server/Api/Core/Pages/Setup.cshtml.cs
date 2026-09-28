using DevInstance.DevCoreApp.Server.Services.Core;
using DevInstance.DevCoreApp.Shared.Model.Core.Account;
using DevInstance.WebServiceToolkit.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DevInstance.DevCoreApp.Server.Api.Core.Pages;

/// <summary>
/// First-run owner setup: the one page the server still renders. Anonymous, and gone (404) as
/// soon as any user exists; the service re-checks that on submit, so a race cannot create a
/// second owner. It does not sign in: the owner logs in through the WASM client afterwards.
/// </summary>
public class SetupModel : PageModel
{
    private readonly IAccountService accounts;

    public SetupModel(IAccountService accounts) => this.accounts = accounts;

    [BindProperty]
    public SetupOwnerParameters Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync() =>
        await IsSetupRequiredAsync() ? Page() : NotFound();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await IsSetupRequiredAsync())
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await accounts.SetupOwnerAsync(Input);
        }
        catch (BadRequestException ex)
        {
            // Identity rejected the account (e.g. password rules): show why, keep the form.
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }

        // The client's login page (Desktop is served at the site root).
        return Redirect("/account/login");
    }

    private async Task<bool> IsSetupRequiredAsync() => (await accounts.IsSetupRequiredAsync()).Result;
}
