using DevInstance.LogScope;

namespace DevInstance.DevCoreApp.Client.Services.Utils;

public delegate Task<ServiceActionResult<T>> PerformAsyncCallHandler<T>();

public class ServiceExecutionHandler
{
    private List<Func<Task<bool>>> tasks;

    private IServiceExecutionHost basePage;

    LastRequestReplayAgent LoginHandler { get; set; }

    IScopeLog log;


    public ServiceExecutionHandler(IScopeLog l, IServiceExecutionHost basePage, LastRequestReplayAgent lastRequestReplay)
    {
        this.log = l.TraceScope("SEHandler");
        this.basePage = basePage;
        this.LoginHandler = lastRequestReplay;

        tasks = new List<Func<Task<bool>>>();
    }

    public ServiceExecutionHandler DispatchCall<T>(PerformAsyncCallHandler<T> handler,
                                                    Action<T> success = null,
                                                    Func<T, Task> sucessAsync = null,
                                                    Action<ServiceActionError[]> error = null,
                                                    Action before = null,
                                                    bool enableProgress = true)
    {
        using (var l = log.TraceScope())
        {
            Func<Task<bool>> task = async () => await PerformServiceCallAsync(handler, success, sucessAsync, error, before, enableProgress);
            tasks.Add(task);
            return this;
        }
    }

    public async Task ExecuteAsync()
    {
        using (var l = log.TraceScope())
        {
            foreach (var task in tasks)
            {
                if (!await task())
                    break;
            }
        }
    }

    private async Task<bool> PerformServiceCallAsync<T>(PerformAsyncCallHandler<T> handler, Action<T> success, Func<T, Task> sucessAsync, Action<ServiceActionError[]> error, Action before, bool enableProgress)
    {
        ServiceActionResult<T> res = null;

        using (var l = log.TraceScope())
        {
            if (enableProgress)
            {
                basePage.InProgress = true;
                basePage.StateHasChanged();
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
                    Errors = new ServiceActionError[] { new ServiceActionError { Message = ex.Message } },
                    Success = false
                };
            }

            basePage.IsError = !res.Success;
            if (res.Success)
            {
                if (success != null)
                {
                    success(res.Result);
                }
                if (sucessAsync != null)
                {
                    await sucessAsync(res.Result);
                }
            }
            else
            {
                var errorMessage = "";
                foreach (var errm in res.Errors)
                {
                    errorMessage += errm;
                }
                l.W(errorMessage);
                basePage.ErrorMessage = errorMessage;

                //If session has expired we show the login popup and try request again
                if (LoginHandler != null && !res.IsAuthorized)
                {
                    LoginHandler.LastRequestReplay = async (i) => { await ExecuteAsync(); };

                    if (enableProgress)
                    {
                        basePage.InProgress = true;
                        basePage.StateHasChanged();
                    }

                    await basePage.ShowLoginModalAsync();

                    return false;
                }
                else if (error != null)
                {
                    error(res.Errors);
                }
            }

            basePage.InProgress = false;
            basePage.StateHasChanged();

            return true;
        }
    }
}
