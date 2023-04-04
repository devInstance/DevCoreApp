using DevInstance.DevCoreApp.Client.Services.Api;
using DevInstance.DevCoreApp.Shared.Model;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Client.UI.Pages;

public partial class FetchData
{
    [Inject]
    IWeatherForecastService Service { get; set; }

    private const int PageSize = 15;
    private ModelList<WeatherForecastItem> forecasts;

    protected override async Task OnInitializedAsync()
    {
        await RequestDataAsync(0);
    }

    protected async Task RequestDataAsync(int page)
    {
        await PerformServiceCallAsync(() => Service.GetItemsAsync(PageSize, page, null, null, null), (a) => { forecasts = a; });
    }
}
