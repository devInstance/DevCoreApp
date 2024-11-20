using DevInstance.DevCoreApp.Components.Account;

//TODO: migrate to view-model
using DevInstance.DevCoreApp.Server.Database.Core.Models;

using DevInstance.DevCoreApp.Server.WebService.Components;
using DevInstance.DevCoreApp.Server.WebService.Tools;
using DevInstance.LogScope.Extensions.MicrosoftLogger;
using DevInstance.LogScope.Formatters;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using DevInstance.DevCoreApp.Server.Database.Postgres;
using DevInstance.DevCoreApp.Server.Database.SqlServer;
using DevInstance.DevCoreApp.Server.WebService.Authentication;
using DevInstance.DevCoreApp.Shared.Services;
using DevInstance.DevCoreApp.Server.WebService.Services;

namespace DevInstance.DevCoreApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddTimeProvider();

#if DEBUG
            builder.Services.AddMicrosoftScopeLogging(DevInstance.LogScope.LogLevel.TRACE, "LScope", new DefaultFormattersOptions { ShowTimestamp = true, ShowThreadNumber = true });
#else
        builder.Services.AddMicrosoftScopeLogging(DevInstance.LogScope.LogLevel.NOLOG, "LScope");
#endif


            // Add services to the container.
#if DEBUG
            builder.Services.AddRazorComponents(options => options.DetailedErrors = builder.Environment.IsDevelopment())
                    .AddInteractiveWebAssemblyComponents()
                    .AddInteractiveServerComponents();
#else
            builder.Services.AddRazorComponents()
                    .AddInteractiveWebAssemblyComponents()
                    .AddInteractiveServerComponents();
#endif

            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<IdentityUserAccessor>();
            builder.Services.AddScoped<IdentityRedirectManager>();
            builder.Services.AddScoped<AuthenticationStateProvider, PersistingServerAuthenticationStateProvider>();

            builder.Services.AddAuthorization();
            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = IdentityConstants.ApplicationScheme;
                    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
                })
                .AddIdentityCookies();

            AddDatabase(builder.Services, builder.Configuration);

#if DEBUG
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
#endif

            builder.Services.AddAppIdentity();

            builder.Services.AddScoped<IWeatherForecastService, WeatherForecastService>();
            builder.Services.AddServerAppServices();
            builder.Services.AddControllers();

            builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
            builder.Services.AddLocalization();

            // Calling client registrations since Main function is not called in the client
            Client.ClientRegistry.Register(builder.Services);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
                app.UseMigrationsEndPoint();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();
            
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

            app.MapControllers();
            // Add additional endpoints required by the Identity /Account Razor components.
            app.MapAdditionalIdentityEndpoints();

            app.Run();
        }

        private const string PostgresProvider = "Postgres";
        private const string SqlServerProvider = "SqlServer";

        private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
        {
            var provider = configuration.GetSection("Database").GetValue(typeof(string), "Provider").ToString();

            if (provider == PostgresProvider)
            {
                services.ConfigurePostgresDatabase(configuration);
                services.ConfigurePostgresIdentityContext();
            }
            else if (provider == SqlServerProvider)
            {
                services.ConfigureSqlServerDatabase(configuration);
                services.ConfigureSqlServerIdentityContext();
            }

        }

    }
}
