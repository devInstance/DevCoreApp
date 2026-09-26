using DevInstance.BlazorToolkit.Http;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Client.Services.Core.Auth;
using DevInstance.DevCoreApp.Client.Services.Core.Notifications;
using DevInstance.DevCoreApp.Client.Services.Core.Time;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace DevInstance.DevCoreApp.Client.Services.Core;

public static class ClientServicesExtensions
{
    /// <summary>
    /// Everything a DevCoreApp WASM client needs to talk to the API: HTTP clients with the JWT
    /// handler, authentication state from <c>api/me</c>, permission policies, local time, the
    /// notification hub, and every <c>[BlazorService]</c> in this assembly.
    /// </summary>
    /// <param name="apiBaseAddress">API origin: the app's own origin when hosted by the server,
    /// or <c>ApiBaseUrl</c> from the client's appsettings when deployed elsewhere.</param>
    public static IServiceCollection AddDevCoreClientServices(this IServiceCollection services, Uri apiBaseAddress)
    {
        var options = new ApiClientOptions(apiBaseAddress);
        services.AddSingleton(options);

        // Session (singletons: HTTP message handlers are built in their own DI scope).
        services.AddSingleton<ITokenStorage, BrowserTokenStorage>();
        services.AddSingleton<AuthTokenStore>();
        services.AddSingleton<TokenRefresher>();
        services.AddTransient<AuthTokenHandler>();

        services.AddHttpClient(ApiClient.HttpClientName, c => c.BaseAddress = options.BaseAddress)
            .AddHttpMessageHandler<AuthTokenHandler>();
        services.AddHttpClient(ApiClient.AuthHttpClientName, c => c.BaseAddress = options.BaseAddress);
        services.AddScoped<IHttpApiContextFactory>(sp =>
            new HttpApiContextFactory(sp.GetRequiredService<IHttpClientFactory>(), ApiClient.HttpClientName));

        services.AddSingleton<ILocalTimeService, LocalTimeService>();

        services.AddAuthorizationCore();
        services.AddSingleton<IAuthorizationPolicyProvider, ClientPermissionPolicyProvider>();
        services.AddCascadingAuthenticationState();
        services.AddScoped<ApiAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthenticationStateProvider>());
        services.AddScoped<ICurrentUserState>(sp => sp.GetRequiredService<ApiAuthenticationStateProvider>());

        services.AddScoped<INotificationHubClient, NotificationHubClient>();
        services.AddScoped<IUnreadNotificationsMonitor, UnreadNotificationsMonitor>();

        services.AddBlazorServices(typeof(ClientServicesExtensions).Assembly);
        return services;
    }
}
