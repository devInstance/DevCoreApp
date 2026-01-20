using DevInstance.BlazorToolkit.Http;
using DevInstance.DevCoreApp.Client.Services;
using DevInstance.DevCoreApp.Client.Services.Net;
using DevInstance.DevCoreApp.Client.Services.Net.Api;
using DevInstance.DevCoreApp.Shared.Services;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope.Extensions;
using DevInstance.LogScope.Formatters;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace DevInstance.DevCoreApp.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

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
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();

        // Read this for configuring HTTP client behavior for your specific use case: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines
        builder.Services.AddHttpClient("DevInstance.DevCoreApp.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
        builder.Services.AddScoped<IHttpApiContextFactory>(
            sp => new HttpApiContextFactory(sp.GetRequiredService<IHttpClientFactory>(), "DevInstance.DevCoreApp.Client", "api")
            );

        builder.Services.AddNetApi();

        builder.Services.AddAppServices();

        await builder.Build().RunAsync();
    }
}
