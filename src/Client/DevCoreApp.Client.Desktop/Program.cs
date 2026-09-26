using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core;
using DevInstance.LogScope.Extensions;
using DevInstance.LogScope.Formatters;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace DevInstance.DevCoreApp.Client.Desktop;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        // Qualified: a bare "App" would bind to the DevInstance.DevCoreApp.Client.Desktop.App namespace (see root CLAUDE.md).
        builder.RootComponents.Add<UI.App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

#if DEBUG
        builder.Services.AddConsoleScopeLogging(LogScope.LogLevel.DEBUG,
            new DefaultFormattersOptions { ShowTimestamp = true, ShowThreadNumber = true, ShowId = true });
#else
        builder.Services.AddConsoleScopeLogging(LogScope.LogLevel.NOLOG,
            new DefaultFormattersOptions { ShowTimestamp = false, ShowThreadNumber = false, ShowId = false });
#endif

        builder.Services.AddDevCoreClientServices(ResolveApiBase(builder));
        builder.Services.AddBlazorServices(typeof(Program).Assembly);

        await builder.Build().RunAsync();
    }

    /// <summary>
    /// <c>ApiBaseUrl</c> when configured; otherwise the origin the app is served from. Origin only —
    /// the app's base path (e.g. <c>/mobile/</c>) is not part of the API URL.
    /// </summary>
    private static Uri ResolveApiBase(WebAssemblyHostBuilder builder)
    {
        var configured = builder.Configuration["ApiBaseUrl"];
        return !string.IsNullOrWhiteSpace(configured)
            ? new Uri(configured)
            : new Uri(new Uri(builder.HostEnvironment.BaseAddress).GetLeftPart(UriPartial.Authority));
    }
}
