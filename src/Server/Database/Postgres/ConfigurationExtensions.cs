using DevInstance.SampleWebApp.Server.Database.Core;
using DevInstance.SampleWebApp.Server.Database.Core.Data;
using DevInstance.SampleWebApp.Server.Database.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevInstance.SampleWebApp.Server.Database.Postgres
{
    public static class ConfigurationExtensions
    {
        public static void ConfigurePostgresDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("PostgresConnection");
            services.AddDbContext<ApplicationDbContext, PostgresApplicationDbContext>(options =>
                    options.UseNpgsql(
                            connectionString,
                            b => b.MigrationsAssembly("DevInstance.SampleWebApp.Server.Database.Postgres")
                            ));
            services.AddScoped<IQueryRepository, PostgresQueryRepository>();
        }

        public static void ConfigurePostgresIdentityContext(this IServiceCollection services)
        {
            services.ConfigureIdentityContext<PostgresApplicationDbContext>();
        }
    }
}
