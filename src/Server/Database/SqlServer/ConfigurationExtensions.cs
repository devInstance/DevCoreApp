using DevInstance.SampleWebApp.Server.Database.Core;
using DevInstance.SampleWebApp.Server.Database.Core.Data;
using DevInstance.SampleWebApp.Server.Database.SqlServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevInstance.SampleWebApp.Server.Database.SqlServer
{
    public static class ConfigurationExtensions
    {
        public static void ConfigureSqlServerDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("SqlServerConnection");
            services.AddDbContext<ApplicationDbContext, SqlServerApplicationDbContext>(options =>
                    options.UseSqlServer(
                            connectionString,
                            b => b.MigrationsAssembly("DevInstance.SampleWebApp.Server.Database.SqlServer")
                            ));

            services.AddScoped<IQueryRepository, SqlServerQueryRepository>();
        }

        public static void ConfigureSqlServerIdentityContext(this IServiceCollection services)
        {
            services.ConfigureIdentityContext<SqlServerApplicationDbContext>();
        }
    }
}
