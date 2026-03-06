using System.Text;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Admin.Services.Background;
using DevInstance.DevCoreApp.Server.Admin.Services.Background.Tasks;
using DevInstance.DevCoreApp.Server.Admin.Services.Background.Tasks.Handlers;
using DevInstance.DevCoreApp.Server.Admin.Services.ImportExport;
using DevInstance.DevCoreApp.Server.Admin.Services.ImportExport.Handlers;
using DevInstance.DevCoreApp.Server.Admin.Services.Notifications;
using DevInstance.DevCoreApp.Server.Admin.Services.Notifications.Templates;
using DevInstance.DevCoreApp.Server.Admin.Services.Seeding;
using DevInstance.DevCoreApp.Server.Admin.Services.UserAdmin;
using DevInstance.DevCoreApp.Server.Admin.WebService.Hubs;
using DevInstance.DevCoreApp.Server.Admin.WebService.Identity;
using DevInstance.DevCoreApp.Server.Admin.WebService.Logging;
using DevInstance.DevCoreApp.Server.Admin.WebService.Health;
using DevInstance.DevCoreApp.Server.Admin.WebService.Middleware;
using DevInstance.DevCoreApp.Server.Admin.WebService.UI;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Postgres;
using DevInstance.DevCoreApp.Server.Database.SqlServer;
using DevInstance.DevCoreApp.Server.EmailProcessor.MailKit;
using DevInstance.DevCoreApp.Server.StorageProcessor.Local;
using DevInstance.DevCoreApp.Shared.Model.Authentication;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope.Extensions.SerilogLogger;
using DevInstance.LogScope.Formatters;
using DevInstance.DevCoreApp.Server.Admin.WebService.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using TimeProvider = DevInstance.DevCoreApp.Shared.Utils.TimeProvider; //TODO: migrate to standard TimeProvider

#if SERVICEMOCKS
using DevInstance.DevCoreApp.Server.Admin.Services.Mocks.UserAdmin;
using DevInstance.DevCoreApp.Server.Admin.Services.Mocks.ImportExport;
#endif

namespace DevInstance.DevCoreApp.Server.Admin.WebService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Serilog: replaces the default Microsoft logging provider.
        // LogScope (IScopeManager/IScopeLog) continues to be the application-level API —
        // it bridges to ILogger which now flows into the Serilog pipeline.
        SerilogConfiguration.ConfigureSerilog(builder);

        // Background task infrastructure
        builder.Services.Configure<BackgroundTaskSettings>(
            builder.Configuration.GetSection(BackgroundTaskSettings.SectionName));
        builder.Services.AddSingleton<IBackgroundTaskHandler, SendEmailTaskHandler>();
        builder.Services.AddSingleton<IBackgroundTaskHandler, ImportDataTaskHandler>();
        builder.Services.AddSingleton<IBackgroundTaskHandler, WebhookDeliveryTaskHandler>();
        builder.Services.AddSingleton<BackgroundTaskWorker>();
        builder.Services.AddSingleton<IBackgroundTaskWorker>(sp => sp.GetRequiredService<BackgroundTaskWorker>());
        builder.Services.AddSingleton<BackgroundWorker>();
        builder.Services.AddSingleton<IBackgroundWorker>(sp => sp.GetRequiredService<BackgroundWorker>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<BackgroundWorker>());

        builder.Services.AddScoped<ITimeProvider, TimeProvider>();

#if DEBUG
        builder.Services.AddSerilogScopeLogging(LogScope.LogLevel.TRACE, new DefaultFormattersOptions { ShowThreadNumber = true });
#else
        builder.Services.AddSerilogScopeLogging(LogScope.LogLevel.INFO);
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

        builder.Services.Configure<JwtSettings>(
            builder.Configuration.GetSection(JwtSettings.SectionName));

        var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);

        builder.Services.AddAuthorization();
        var authBuilder = builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Smart";
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddPolicyScheme("Smart", "Smart", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    if (context.Request.Headers.ContainsKey("X-Api-Key"))
                        return "ApiKey";

                    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                    if (authHeader != null &&
                        authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        return JwtBearerDefaults.AuthenticationScheme;
                    return IdentityConstants.ApplicationScheme;
                };
            });

        authBuilder.AddIdentityCookies();
        authBuilder.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", null);
        authBuilder.AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSection["Secret"]!)),
                ClockSkew = TimeSpan.FromMinutes(1),
            };

            // SignalR sends JWT as a query parameter ("access_token") for WebSocket connections
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        AddDatabase(builder.Services, builder.Configuration);
        builder.Services.AddScoped<IDataSeeder, OrganizationDataSeeder>();
        builder.Services.AddScoped<IDataSeeder, PermissionSeeder>();

#if DEBUG || SERVICEMOCKS
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();
#endif

        builder.Services.AddAppIdentity();

        // IOperationContext — provides user/org/tracing context to the data layer.
        // The factory lambda auto-selects the implementation: HttpOperationContext when
        // running inside an HTTP request, BackgroundOperationContext otherwise (worker jobs).
        builder.Services.AddScoped<HttpOperationContext>();
        builder.Services.AddScoped<BackgroundOperationContext>();
        builder.Services.AddScoped<IOperationContext>(sp =>
        {
            var accessor = sp.GetRequiredService<IHttpContextAccessor>();
            return accessor.HttpContext != null
                ? sp.GetRequiredService<HttpOperationContext>()
                : sp.GetRequiredService<BackgroundOperationContext>();
        });

#if !SERVICEMOCKS
        builder.Services.AddBlazorServices();
        builder.Services.AddBlazorServices(typeof(UserProfileService).Assembly);

        // Import/Export handlers
        builder.Services.AddScoped<IImportHandler, UserProfileImportHandler>();
        builder.Services.AddScoped<IExportHandler, UserProfileExportHandler>();
#else
        builder.Services.AddBlazorServicesMocks();
        builder.Services.AddBlazorServicesMocks(typeof(UserProfileServiceMock).Assembly);
        builder.Services.AddBlazorServicesMocks(typeof(UserProfileService).Assembly);
#endif

        builder.Services.AddHttpClient("WebhookDelivery", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        builder.Services.AddSignalR();
        builder.Services.AddScoped<INotificationHubService, NotificationHubService>();

        builder.Services.AddControllers();
        builder.Services.AddMailKit(builder.Configuration);
        builder.Services.AddLocalFileStorage(builder.Configuration);
        builder.Services.AddSingleton<IEmailTemplateService, EmailTemplateService>(); //TODO: use webservice toolkit for it
        builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityEmailSender>();
        builder.Services.AddLocalization();

        builder.Services.AddExceptionHandler<ApiExceptionHandler>();

        builder.Services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" })
            .AddCheck<BackgroundWorkerHealthCheck>("background-worker", tags: new[] { "ready" })
            .AddCheck<StuckEmailsHealthCheck>("stuck-emails", tags: new[] { "ready" });

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
            // Production: ApiExceptionHandler (registered via AddExceptionHandler<T>) runs
            // first inside UseExceptionHandler — handles API paths with JSON, falls through
            // to /Error page for non-API paths.
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseCorrelationId();
        app.UseSerilogRequestLogging();

        app.UseStaticFiles();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.MapControllers();
        app.MapHub<NotificationHub>("/hubs/notifications");
        // Add additional endpoints required by the Identity /Account Razor components.
        app.MapAdditionalIdentityEndpoints();

        // Health check endpoints
        app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false, // Liveness: no dependency checks
            ResponseWriter = HealthCheckResponseWriter.WriteAsync
        });
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = HealthCheckResponseWriter.WriteAsync
        });

        // Apply pending migrations, seed roles and data
        await app.Services.MigrateAndSeedAsync();

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
