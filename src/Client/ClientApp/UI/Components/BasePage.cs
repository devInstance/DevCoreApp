using DevInstance.DevCoreApp.Client.Services;
using DevInstance.LogScope;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace DevInstance.DevCoreApp.Client.UI.Components;

public class BasePage : ComponentBase
{
    [Inject]
    protected IScopeManager ScopeManager { get; set; }

    private IScopeLog log;

    protected override void OnInitialized()
    {
        log = ScopeManager.CreateLogger(this);
    }

    public delegate Task<ServiceActionResult<T>> PerformAsyncCallHandler<T>();

    protected bool InProgress { get; set; } = false;
    protected bool IsError { get; set; } = false;
    protected string ErrorMessage { get; set; } = "";

    protected async Task<T> PerformServiceCallAsyncSuccessAsync<T>(PerformAsyncCallHandler<T> handler, Func<T, Task> successAsync, Action<string> error = null, bool enableProgress = true)
    {
        return await PerformServiceCallAsync(handler, null, null, successAsync, error, enableProgress);
    }

    protected async Task<T> PerformServiceCallAsync<T>(PerformAsyncCallHandler<T> handler, Action<T> success = null, Action<string> error = null, bool enableProgress = true)
    {
        return await PerformServiceCallAsync(handler, null, success, null, error, enableProgress);
    }

    /// <summary>
    /// Call service method async with ServiceActionResult return type
    /// </summary>
    /// <typeparam name="T">Model type</typeparam>
    /// <param name="handler">action hander</param>
    /// <param name="successAsync">async action called in case of success</param>
    /// <param name="enableProgress">change progress flag</param>
    /// <returns></returns>
    protected async Task<T> PerformServiceCallAsync<T>(PerformAsyncCallHandler<T> handler, Action before, Action<T> success, Func<T, Task> sucessAsync, Action<string> error, bool enableProgress)
    {
        using (var l = log.TraceScope())
        {
            ServiceActionResult<T> res = null;
            
            if (enableProgress)
            {
                InProgress = true;
                StateHasChanged();
            }
            
            if (before != null)
            {
                before();
            }

            try
            {
                res = await handler();
            }
            catch (Exception ex)
            {
                l.E(ex.Message);
                l.I(ex.StackTrace);
                res = new ServiceActionResult<T>
                {
                    ErrorMessage = ex.Message,
                    Success = false
                };
            }

            IsError = !res.Success;
            if (res.Success)
            {
                if(success != null)
                {
                    success(res.Result);
                }
                if(sucessAsync != null)
                {
                    await sucessAsync(res.Result);
                }
            }
            else
            {
                ErrorMessage = res.ErrorMessage;
                l.W(ErrorMessage);
                if (error != null)
                {
                    error(res.ErrorMessage);
                }
            }

            InProgress = false;
            StateHasChanged();

            return res.Success ? res.Result : default(T);
        }
    }
}