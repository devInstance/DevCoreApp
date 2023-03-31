using DevInstance.LogScope.Extensions.MicrosoftLogger;
using DevInstance.LogScope.Formatters;
using DevInstance.DevCoreApp.Server.Database.Postgres;
using DevInstance.DevCoreApp.Server.Database.SqlServer;
using DevInstance.DevCoreApp.Server.EmailProcessor.MailKit;
using DevInstance.DevCoreApp.Server.WebService.Indentity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DevInstance.DevCoreApp.Server.WebService.Tools;

namespace DevInstance.DevCoreApp.Server;

public class Startup
{
    private const string PostgresProvider = "Postgres";
    private const string SqlServerProvider = "SqlServer";

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTimeProvider();

#if DEBUG
        services.AddMicrosoftScopeLogging(DevInstance.LogScope.LogLevel.TRACE, "LScope", new DefaultFormattersOptions { ShowTimestamp = true, ShowThreadNumber = true });
#else
        services.AddMicrosoftScopeLogging(DevInstance.LogScope.LogLevel.NOLOG, "LScope");
#endif

        AddDatabase(services);

        services.AddIdentity();

        services.AddMailKit(Configuration);
        
        services.AddAppServices();

        services.AddControllers().AddNewtonsoftJson();

        services.AddSwaggerGen();
    }

    private void AddDatabase(IServiceCollection services)
    {
        var provider = Configuration.GetSection("Database").GetValue(typeof(string), "Provider").ToString();

        if (provider == PostgresProvider)
        {
            services.ConfigurePostgresDatabase(Configuration);
            services.ConfigurePostgresIdentityContext();
        }
        else if (provider == SqlServerProvider)
        {
            services.ConfigureSqlServerDatabase(Configuration);
            services.ConfigureSqlServerIdentityContext();
        }

    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseMigrationsEndPoint();

            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
