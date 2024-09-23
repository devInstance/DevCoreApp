using DevInstance.DevCoreApp.Client.Services;
using DevInstance.DevCoreApp.Client.Services.Utils;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Client.UI.Pages.Authentication;

public class AuthBasePage : ComponentBase, IServiceExecutionHost
{
    public bool InProgress { get; set; } = false;
    public bool IsError { get; set; } = false;
    public string ErrorMessage { get; set; } = "";

    public async Task ShowLoginModalAsync()
    {
        //Do nothing
    }

    protected ServiceExecutionHandler BeginServiceCall()
    {
        return new ServiceExecutionHandler(null, this, null);
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
