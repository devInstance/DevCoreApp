using DevInstance.LogScope;
using DevInstance.LogScope.Extensions;
using DevInstance.LogScope.Formatters;
using DevInstance.SampleWebApp.Client.Net.Api;
using DevInstance.SampleWebApp.Client.Net;
using DevInstance.SampleWebApp.Client.Services;
using DevInstance.SampleWebApp.Client.UI;
using DevInstance.SampleWebApp.Shared.Utils;
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
using DevInstance.SampleWebApp.Client.Extensions;

namespace DevInstance.SampleWebApp.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddLocalization(opts => { opts.ResourcesPath = "Resources"; });

            builder.Services.AddHttpClient("DevInstance.SampleWebApp.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
                //.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

            // Supply HttpClient instances that include access tokens when making requests to the server project
            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("DevInstance.SampleWebApp.ServerAPI"));

            builder.Services.AddApiAuthorization();
            builder.Services.AddScoped<IAuthorizationApi, AuthorizationApi>();
            builder.Services.AddScoped<IUserProfileApi, UserProfileApi>();

            if(builder.HostEnvironment.IsDevelopment())
            {
                builder.Services.AddConsoleScopeLogging(LogLevel.INFO, new DefaultFormattersOptions { ShowTimestamp = true, ShowThreadNumber = true });
            }
            else
            {
                builder.Services.AddConsoleScopeLogging(LogLevel.ERROR);
            }

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
