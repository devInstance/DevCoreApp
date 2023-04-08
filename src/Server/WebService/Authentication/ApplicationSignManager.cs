using DevInstance.DevCoreApp.Server.Database.Core.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.WebService.Authentication;

public class ApplicationSignManager : IApplicationSignManager
{
    SignInManager<ApplicationUser> signInManager;

    public ApplicationSignManager(SignInManager<ApplicationUser> signInManager)
    {
        this.signInManager = signInManager;
    }

    public async Task<SignInResult> SignInAsync(ApplicationUser user, string password, bool persistent)
    {
        var singInResult = await signInManager.CheckPasswordSignInAsync(user, password, false);

        if (singInResult.Succeeded)
        {
            await signInManager.SignInAsync(user, persistent);
        }

        return singInResult;
    }

    public Task SignOutAsync()
    {
        return signInManager.SignOutAsync();
    }
}
