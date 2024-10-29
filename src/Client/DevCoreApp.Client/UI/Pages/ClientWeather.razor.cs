using DevInstance.DevCoreApp.Client.Services.Api;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.LogScope;
using Microsoft.AspNetCore.Components;
using System.Net.Http;

namespace DevInstance.DevCoreApp.Client.UI.Pages;

public partial class ClientWeather
{
    [Inject]
    protected IScopeManager ScopeManager { get; set; }

    [Inject]
    private IWeatherForecastService Service { get; set; }
    
    private const int PageSize = 15;

    private IScopeLog log;

    private ModelList<WeatherForecastItem> forecasts;

    private WeatherForecastItem selectedForecast;
    private WeatherForecastFields selectedSortBy = WeatherForecastFields.Date;
    private bool selectedIsAsc = true;

   
    protected override async Task OnInitializedAsync()
    {
        log = ScopeManager.CreateLogger(this);
        using (var scope = log.TraceScope())
        {
            await RequestDataAsync(0);
            Service.OnDataUpdate += Service_OnDataUpdate;
        }
    }

    private async Task Service_OnDataUpdate(WeatherForecastItem item)
    {
        using (var scope = log.TraceScope())
        {
            await RequestDataAsync(0);
        }
    }

    protected async Task RequestDataAsync(int page)
    {
        await ServiceCallAsync(() => Service.GetItemsAsync(PageSize, page, null, null, null), (a) => { forecasts = a; });
    }

    private async Task Remove(WeatherForecastItem item)
    {
        await ServiceCallAsync(() => Service.RemoveAsync(item), null, async (a) => { await RequestDataAsync(forecasts.Page); });
    }

    private async Task SortBy(WeatherForecastFields sortBy)
    {
        if (selectedSortBy != sortBy)
        {
            selectedIsAsc = true;
        }
        else
        {
            selectedIsAsc = !selectedIsAsc;
        }

        await ServiceCallAsync(
            () => Service.GetItemsAsync(PageSize, forecasts?.Page ?? 0, sortBy, selectedIsAsc, null), 
            (a) => { 
                forecasts = a;
                selectedSortBy = sortBy;
            });
    }
}
