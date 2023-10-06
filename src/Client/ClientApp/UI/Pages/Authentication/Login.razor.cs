using DevInstance.DevCoreApp.Shared.Model;
using System.Threading.Tasks;
using System;
using DevInstance.DevCoreApp.Client.Auth;
using Microsoft.AspNetCore.Components;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DevInstance.DevCoreApp.Client.UI.Pages.Authentication;

public partial class Login
{
    [Inject]
    NavigationManager navigationManager { get; set; }

    [Inject]
    IdentityAuthenticationStateProvider authStateProvider { get; set; }

    LoginParameters loginParameters { get; set; } = new LoginParameters();

    string error { get; set; }

    async Task OnSubmit()
    {
        error = null;
        try
        {
            await authStateProvider.LoginAsync(loginParameters);
            navigationManager.NavigateTo("");
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
    }
}
