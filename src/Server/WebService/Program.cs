using DevInstance.DevCoreApp.Server.WebService.Components.Account;

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

namespace DevInstance.DevCoreApp;

public class Program
{
    public static async Task Main(string[] args)
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
                .AddInteractiveServerComponents();
#else
        builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
#endif

        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<IdentityRedirectManager>();
        builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

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
            .AddInteractiveServerRenderMode();

        app.MapControllers();
        // Add additional endpoints required by the Identity /Account Razor components.
        app.MapAdditionalIdentityEndpoints();

        // Seed roles
        await app.Services.SeedRolesAsync();

        await app.RunAsync();
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
