using DevInstance.DevCoreApp.Client.Services.Api;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.LogScope;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

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
            await ServiceCallAsync(() => Service.AddNewAsync(
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
                    //TODO: Show error
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

