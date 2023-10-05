using DevInstance.DevCoreApp.Client.Services;
using DevInstance.DevCoreApp.Client.Services.Utils;
using DevInstance.LogScope;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Client.UI.Components;

public class BasePage : ComponentBase, IServiceExecutionHost
{
    [Inject]
    protected IScopeManager ScopeManager { get; set; }

    [Inject]
    IJSRuntime JJSRuntime { get; set; }

    [Inject]
    LastRequestReplayAgent LastRequestReplay { get; set; }

    private IScopeLog log;

    protected override void OnInitialized()
    {
        log = ScopeManager.CreateLogger(this);
    }

    public bool InProgress { get; set; } = false;
    public bool IsError { get; set; } = false;
    public string ErrorMessage { get; set; } = "";

    protected ServiceExecutionHandler BeginServiceCall()
    {
        return new ServiceExecutionHandler(log, this, LastRequestReplay);
    }

    public async Task ShowLoginModalAsync()
    {
        await JJSRuntime.InvokeAsync<bool>("showBootstrapModal", "loginModal");
    }

    protected async Task ServiceCallAsync<T>(PerformAsyncCallHandler<T> handler, Action<T> success = null, Func<T, Task> sucessAsync = null, Action<ServiceActionError[]> error = null, Action before = null, bool enableProgress = true)
    {
        await BeginServiceCall().DispatchCall<T>(handler, success, sucessAsync, error, before, enableProgress).ExecuteAsync();
    }

    void IServiceExecutionHost.StateHasChanged()
    {
        StateHasChanged();
    }
}
