using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevInstance.DevCoreApp.Server.Database.Postgres;

public static class ConfigurationExtensions
{
    public static void ConfigurePostgresDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgresConnection");
        services.AddDbContext<ApplicationDbContext, PostgresApplicationDbContext>(options =>
                options.UseNpgsql(
                        connectionString,
                        b => b.MigrationsAssembly("DevInstance.DevCoreApp.Server.Database.Postgres")
                        ));

        // Scoped repository over the ambient DI-scoped context — for code that already opens its
        // own scope per operation (background workers, import handlers, HttpOperationContext,
        // seeders, Identity). ownsContext defaults to false: DI owns and disposes that context.
        services.AddScoped<IQueryRepository, PostgresQueryRepository>();

        // Per-operation unit-of-work infrastructure (Blazor Server concurrency safety).
        // Options are built once; the scoped factory binds each created context to the live
        // IOperationContext. See src/Server/Database/UnitOfWork.md.
        var unitOfWorkOptions = new DbContextOptionsBuilder<PostgresApplicationDbContext>()
                .UseNpgsql(
                        connectionString,
                        b => b.MigrationsAssembly("DevInstance.DevCoreApp.Server.Database.Postgres"))
                .Options;
        services.AddScoped<IAppDbContextFactory>(sp =>
                new PostgresAppDbContextFactory(unitOfWorkOptions, sp.GetRequiredService<IOperationContext>()));
        services.AddScoped<IQueryRepositoryFactory, QueryRepositoryFactory>();
    }

    public static void ConfigurePostgresIdentityContext(this IServiceCollection services)
    {
        services.ConfigureIdentityContext<PostgresApplicationDbContext>();
    }

}
