using Microsoft.AspNetCore.Components;
using System.Net.Http;

namespace DevInstance.DevCoreApp.Client.Net;

public class ApiBase
{
    protected readonly HttpClient httpClient;
    public ApiBase(HttpClient http, NavigationManager navigationManager)
    {
        httpClient = http;
        httpClient.BaseAddress = new Uri(navigationManager.BaseUri);
    }
}
