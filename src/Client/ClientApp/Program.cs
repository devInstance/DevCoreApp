using DevInstance.LogScope.Extensions;
using DevInstance.LogScope.Formatters;
using DevInstance.DevCoreApp.Client.UI;
using DevInstance.DevCoreApp.Shared.Utils;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using DevInstance.DevCoreApp.Client.Extensions;
using DevInstance.DevCoreApp.Client.Services;
using DevInstance.DevCoreApp.Client.Auth;
using DevInstance.DevCoreApp.Client.Net;
using Microsoft.AspNetCore.Components.Authorization;

namespace DevInstance.DevCoreApp.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");

        builder.Services.AddLocalization(opts => { opts.ResourcesPath = "Resources"; });

        builder.Services.AddHttpClient("DevInstance.DevCoreApp.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
            //.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

        // Supply HttpClient instances that include access tokens when making requests to the server project
        builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("DevInstance.DevCoreApp.ServerAPI"));

#if DEBUG
        builder.Services.AddConsoleScopeLogging(LogScope.LogLevel.DEBUG,
            new DefaultFormattersOptions { ShowTimestamp = true, ShowThreadNumber = true, ShowId = true });
#else
        builder.Services.AddConsoleScopeLogging(LogScope.LogLevel.NOLOG,
           new DefaultFormattersOptions { ShowTimestamp = false, ShowThreadNumber = false, ShowId = false});
#endif

        builder.Services.AddScoped<AuthenticationStateProvider>(s => s.GetRequiredService<IdentityAuthenticationStateProvider>());
        builder.Services.AddScoped<IdentityAuthenticationStateProvider>();

        builder.Services.AddApiAuthorization();

        builder.Services.AddSingleton<ITimeProvider, TimeProvider>();

#if NETAPIMOQS
            builder.Services.AddMoqNetApi();
#else
        builder.Services.AddNetApi();
#endif

#if SERVICEMOQS
            builder.Services.AddMoqAppServices();
#else
        builder.Services.AddAppServices();
#endif

        var host = builder.Build();

        await host.SetDefaultCulture();

        await host.RunAsync();
    }
}
