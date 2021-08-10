using DevInstance.LogScope.Extensions.MicrosoftLogger;
using DevInstance.LogScope.Formatters;
using DevInstance.SampleWebApp.Server.Database.Postgres;
using DevInstance.SampleWebApp.Server.Database.SqlServer;
using DevInstance.SampleWebApp.Server.EmailProcessor.MailKit;
using DevInstance.SampleWebApp.Server.Indentity;
using DevInstance.SampleWebApp.Server.Services;
using DevInstance.SampleWebApp.Shared.Utils;
using Microsoft.AspNetCore.Authentication;
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

            services.AddScoped<IApplicationSignManager, ApplicationSignManager>();

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
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                //app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseIdentityServer();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
