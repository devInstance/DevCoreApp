using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.LogScope.Extensions;
using DevInstance.LogScope.Formatters;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace DevInstance.DevCoreApp.Client.Mobile;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        // Qualified: a bare "App" would bind to the DevInstance.DevCoreApp.Client.Mobile.App namespace (see root CLAUDE.md).
        builder.RootComponents.Add<UI.App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

#if DEBUG
        builder.Services.AddConsoleScopeLogging(LogScope.LogLevel.DEBUG,
            new DefaultFormattersOptions { ShowTimestamp = true, ShowThreadNumber = true, ShowId = true });
#else
        builder.Services.AddConsoleScopeLogging(LogScope.LogLevel.NOLOG,
            new DefaultFormattersOptions { ShowTimestamp = false, ShowThreadNumber = false, ShowId = false });
#endif

        builder.Services.AddLocalization();

        // The same client services as the Desktop app: auth, api/me, local time, notifications.
        builder.Services.AddDevCoreClientServices(ApiBaseAddress.Resolve(builder.Configuration, builder.HostEnvironment.BaseAddress));
        builder.Services.AddBlazorServices(typeof(Program).Assembly);

        await builder.Build().RunAsync();
    }
}
