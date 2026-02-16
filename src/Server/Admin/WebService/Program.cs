using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Admin.Services.Background;
using DevInstance.DevCoreApp.Server.Admin.Services.Notifications;
using DevInstance.DevCoreApp.Server.Admin.Services.Notifications.Templates;
using DevInstance.DevCoreApp.Server.Admin.Services.UserAdmin;
using DevInstance.DevCoreApp.Server.Admin.WebService.Identity;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Postgres;
using DevInstance.DevCoreApp.Server.Database.SqlServer;
using DevInstance.DevCoreApp.Server.EmailProcessor.MailKit;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope.Extensions.MicrosoftLogger;
using DevInstance.LogScope.Formatters;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using TimeProvider = DevInstance.DevCoreApp.Shared.Utils.TimeProvider; //TODO: migrate to standard TimeProvider

#if SERVICEMOCKS
using DevInstance.DevCoreApp.Server.Admin.Services.Mocks.UserAdmin;
#endif

namespace DevInstance.DevCoreApp.Server.Admin.WebService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<BackgroundWorker>();
        builder.Services.AddSingleton<IBackgroundWorker>(sp => sp.GetRequiredService<BackgroundWorker>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<BackgroundWorker>());

        builder.Services.AddScoped<ITimeProvider, TimeProvider>();
#if DEBUG
        builder.Services.AddMicrosoftScopeLogging(DevInstance.LogScope.LogLevel.TRACE, "LScope", new DefaultFormattersOptions { ShowTimestamp = true, ShowThreadNumber = true });
#else
    builder.Services.AddMicrosoftScopeLogging(DevInstance.LogScope.LogLevel.NOLOG, "LScope");
#endif


        // Add services to the container.
#if DEBUG || SERVICEMOCKS
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

#if DEBUG || SERVICEMOCKS
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();
#endif

        builder.Services.AddAppIdentity();

#if !SERVICEMOCKS
        builder.Services.AddBlazorServices();
        builder.Services.AddBlazorServices(typeof(UserProfileService).Assembly);
#else
        builder.Services.AddBlazorServicesMocks();
        builder.Services.AddBlazorServicesMocks(typeof(UserProfileServiceMock).Assembly);
        builder.Services.AddBlazorServicesMocks(typeof(UserProfileService).Assembly);
#endif

        builder.Services.AddControllers();
        builder.Services.AddMailKit(builder.Configuration);
        builder.Services.AddSingleton<IEmailTemplateService, EmailTemplateService>(); //TODO: use webservice toolkit for it
        builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityEmailSender>();
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

    /// <summary>
    /// This method just to demostrate how we can support multiple database providers. It reads the provider name from configuration and registers the corresponding services.
    /// In real life scenario you would probably need only one of them, so you can just configure a specific database provider directly without this extra level of indirection.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetSection("Database").GetValue(typeof(string), "Provider").ToString();

        if (provider == "Postgres")
        {
            services.ConfigurePostgresDatabase(configuration);
            services.ConfigurePostgresIdentityContext();
        }
        else if (provider == "SqlServer")
        {
            services.ConfigureSqlServerDatabase(configuration);
            services.ConfigureSqlServerIdentityContext();
        }

    }

}
