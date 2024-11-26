using DevInstance.BlazorToolkit.Services;
using DevInstance.LogScope;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Client.UI.Components;

public class BasePage : ComponentBase, IServiceExecutionHost
{
    [Inject]
    protected IScopeManager ScopeManager { get; set; }

    [Inject]
    NavigationManager NavigationManager { get; set; }

    private IScopeLog log;

    protected override void OnInitialized()
    {
        log = ScopeManager.CreateLogger(this);
    }

    /// <summary>
    /// Flag to indicate if the service call is in progress
    /// </summary>
    public bool InProgress { get; set; } = false;
    /// <summary>
    /// Flag to indicate if the service call has an error
    /// </summary>
    public bool IsError { get; set; } = false;
    /// <summary>
    /// Error message from the service call
    /// </summary>
    public string ErrorMessage { get; set; } = "";

    protected ServiceExecutionHandler BeginServiceCall()
    {
        return new ServiceExecutionHandler(log, this);
    }

    public void ShowLogin()
    {
        NavigationManager.NavigateTo($"account/login?returnUrl={Uri.EscapeDataString(NavigationManager.Uri)}");
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
