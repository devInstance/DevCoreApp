using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using System;
using DevInstance.LogScope;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.DevCoreApp.Client.Auth;
using DevInstance.DevCoreApp.Client.Services.Utils;

namespace DevInstance.DevCoreApp.Client.UI.Components.Modals;

public partial class LoginModalOverlay
{
    LoginParameters loginParameters { get; set; } = new LoginParameters();

    [Inject]
    NavigationManager NavigationManager { get; set; }

    [Inject]
    IdentityAuthenticationStateProvider AuthStateProvider { get; set; }

    [Inject]
    protected IScopeManager ScopeManager { get; set; }

    [Inject]
    LastRequestReplayAgent LastRequestReplay { get; set; }

    string Error { get; set; }

    private IScopeLog log;

    protected override void OnInitialized()
    {
        log = ScopeManager.CreateLogger(this);
    }

    public async Task OnSubmit()
    {
        using (var l = log.TraceScope())
        {

            Error = null;
            try
            {
                await AuthStateProvider.LoginAsync(loginParameters);

                await LastRequestReplay.Execute();

            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }
    }

    public async Task OnForgotPassword()
    {
        NavigationManager.NavigateTo("/authentication/forgot-password");
    }
}
