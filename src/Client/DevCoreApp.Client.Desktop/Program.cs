using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Desktop.Core.UI;
using DevInstance.DevCoreApp.Client.Services.Core;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Client.Services.Core.Time;
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

        builder.Services.AddDevCoreClientServices(ApiBaseAddress.Resolve(builder.Configuration, builder.HostEnvironment.BaseAddress));
        builder.Services.AddBlazorServices(typeof(Program).Assembly);

#if SERVICEMOCKS
        // `dotnet run -c ServiceMocks`: in-memory services, no server. Registered after the real
        // ones so they win; sign in with any email and password.
        builder.Services.AddBlazorServicesMocks(typeof(DevInstance.DevCoreApp.Client.Services.Mocks.Core.Auth.AuthServiceMock).Assembly);
#endif

        var host = builder.Build();

        // Grid column definitions format dates through this static facade (see LocalClock).
        LocalClock.Service = host.Services.GetRequiredService<ILocalTimeService>();

        await host.RunAsync();
    }
}
