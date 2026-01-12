using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.DevCoreApp.Shared.Services;
using DevInstance.LogScope;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DevInstance.DevCoreApp.Client.UI.Components.Modals;

public partial class NewForecastModal
{
    [Inject]
    IScopeManager ScopeManager { get; set; }

    [Inject]
    IWeatherForecastService Service { get; set; }

    [Inject]
    IJSRuntime JSRuntime { get; set; }

    [Parameter]
    public WeatherForecastItem ForecastItem { get; set; }

    private IScopeLog log;

    int Temperature { get; set; }

    DateTime Date { get; set; }

    string Summary { get; set; }

    private void ResetForm()
    {
        Temperature = 0;
        Date = DateTime.Now;
        Summary = "";
        ErrorMessage = "";
    }

    protected override void OnInitialized()
    {
        log = ScopeManager.CreateLogger(this);
        using (var l = log.TraceScope())
        {
            ResetForm();
        }
    }

    protected override void OnParametersSet()
    {
        using (var l = log.TraceScope())
        {
            if (ForecastItem != null)
            {
                Temperature = ForecastItem.TemperatureC;
                Date = ForecastItem.Date.ToLocalTime();
                Summary = ForecastItem.Summary;
            }
        }
    }

    private async Task OnSubmit()
    {
        using (var l = log.TraceScope())
        {
            await this.ServiceSubmitAsync(() => Service.AddNewAsync(
                new WeatherForecastItem
                {
                    TemperatureC = Temperature,
                    Date = Date.ToUniversalTime(),
                    Summary = Summary
                }),
                null,
                async (a) =>
                {
                    await OnCloseAsync();
                },
                (e) =>
                {
                    return true;
                }
            );
        }
    }

    private async Task OnCloseAsync()
    {
        ResetForm();
        await JSRuntime.InvokeAsync<bool>("dismissBootstrapModal", "addForecastModal");
    }
}

