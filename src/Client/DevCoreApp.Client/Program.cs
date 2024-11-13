using DevInstance.DevCoreApp.Client.Services;
using DevInstance.DevCoreApp.Client.Services.Net;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope.Extensions;
using DevInstance.LogScope.Formatters;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace DevInstance.DevCoreApp.Client;

internal class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.Services.AddLocalization();

#if DEBUG
        builder.Services.AddConsoleScopeLogging(LogScope.LogLevel.DEBUG,
            new DefaultFormattersOptions { ShowTimestamp = true, ShowThreadNumber = true, ShowId = true });
#else
    builder.Services.AddConsoleScopeLogging(LogScope.LogLevel.NOLOG,
       new DefaultFormattersOptions { ShowTimestamp = false, ShowThreadNumber = false, ShowId = false});
#endif

        builder.Services.AddSingleton<ITimeProvider, Shared.Utils.TimeProvider>();

        builder.Services.AddAuthorizationCore();

        ClientRegistry.Register(builder.Services);
        await builder.Build().RunAsync();
    }
}

/// <summary>
/// This class is used to register all services used by the client. It is called from the Main function here or ASP.NET Service in BlazorApp.
/// </summary>
public class ClientRegistry
{
    public static void Register(IServiceCollection services)
    {
        services.AddCascadingAuthenticationState();
        services.AddScoped<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();

        services.AddConsoleScopeLogging(LogScope.LogLevel.TRACE,
            new DefaultFormattersOptions { ShowTimestamp = true, ShowThreadNumber = true, ShowId = true });

        services.AddHttpClient("DevInstance.DevCoreApp.ServerAPI"/*, client => client.BaseAddress = new Uri(host)*/);

        // Supply HttpClient instances that include access tokens when making requests to the server project
        services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("DevInstance.DevCoreApp.ServerAPI"));

        services.AddAppServices();
        services.AddNetApi();
    }
}
