using DevInstance.LogScope.Extensions.MicrosoftLogger;
using DevInstance.LogScope.Formatters;
using DevInstance.SampleWebApp.Server.Database.Postgres;
using DevInstance.SampleWebApp.Server.Database.SqlServer;
using DevInstance.SampleWebApp.Server.EmailProcessor.MailKit;
using DevInstance.SampleWebApp.Server.Services;
using DevInstance.SampleWebApp.Server.WebService.Indentity;
using DevInstance.SampleWebApp.Shared.Utils;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevInstance.SampleWebApp.Server
{
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
            services.AddSingleton<ITimeProvider, TimeProvider>();

            services.AddMicrosoftScopeLogging(new DefaultFormattersOptions { ShowTimestamp = true, ShowThreadNumber = true });

            ConfigureDatabase(services);

            services.ConfigureIdentity();

            services.ConfigureMailKit(Configuration);
            services.ConfigureServices();

            services.AddControllersWithViews().AddNewtonsoftJson();
            //services.AddRazorPages(); //???
        }

        private void ConfigureDatabase(IServiceCollection services)
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

            services.AddDatabaseDeveloperPageExceptionFilter();
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                //app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            if (!env.IsDevelopment())
            {
                app.UseSpaStaticFiles();
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "ClientApp";
                if (env.IsDevelopment())
                {
                    spa.UseAngularCliServer(npmScript: "start");
                }
            });
        }
    }
}
