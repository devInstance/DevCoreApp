using DevInstance.LogScope.Extensions;
using DevInstance.LogScope.Formatters;
using DevInstance.DevCoreApp.Client.Net.Api;
using DevInstance.DevCoreApp.Client.Net;
using DevInstance.DevCoreApp.Client.Services;
using DevInstance.DevCoreApp.Client.UI;
using DevInstance.DevCoreApp.Shared.Utils;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DevInstance.DevCoreApp.Client.Extensions;

namespace DevInstance.DevCoreApp.Client
{
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

            builder.Services.AddApiAuthorization();
            builder.Services.AddScoped<IAuthorizationApi, AuthorizationApi>();
            builder.Services.AddScoped<IUserProfileApi, UserProfileApi>();

#if DEBUG
            builder.Services.AddConsoleScopeLogging(LogScope.LogLevel.DEBUG,
                new DefaultFormattersOptions { ShowTimestamp = true, ShowThreadNumber = true, ShowId = true });
#else
            builder.Services.AddConsoleScopeLogging(LogScope.LogLevel.NOLOG,
               new DefaultFormattersOptions { ShowTimestamp = false, ShowThreadNumber = false, ShowId = false});
#endif

            builder.Services.AddSingleton<ITimeProvider, TimeProvider>();

            builder.Services.AddScoped<IdentityAuthenticationStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(s => s.GetRequiredService<IdentityAuthenticationStateProvider>());

            builder.Services.AddScoped<AuthorizationService>();
            builder.Services.AddScoped<AccountService>();
            builder.Services.AddScoped<ToolbarService>();
            builder.Services.AddScoped<SettingsService>();

            var host = builder.Build();

            await host.SetDefaultCulture();

            await host.RunAsync();
        }
    }
}
