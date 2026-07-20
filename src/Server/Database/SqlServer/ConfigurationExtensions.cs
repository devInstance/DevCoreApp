using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Postgres.Data;
using DevInstance.DevCoreApp.Server.Database.SqlServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevInstance.DevCoreApp.Server.Database.SqlServer;

public static class ConfigurationExtensions
{
    public static void ConfigureSqlServerDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SqlServerConnection");
        services.AddDbContext<ApplicationDbContext, SqlServerApplicationDbContext>(options =>
                options.UseSqlServer(
                        connectionString,
                        b => b.MigrationsAssembly("DevInstance.DevCoreApp.Server.Database.SqlServer")
                        ));

        // Scoped repository over the ambient DI-scoped context — for code that already opens its
        // own scope per operation (background workers, import handlers, HttpOperationContext,
        // seeders, Identity). ownsContext defaults to false: DI owns and disposes that context.
        services.AddScoped<IQueryRepository, SqlServerQueryRepository>();

        // Per-operation unit-of-work infrastructure (Blazor Server concurrency safety).
        // Options are built once; the scoped factory binds each created context to the live
        // IOperationContext. See src/Server/Database/UnitOfWork.md.
        var unitOfWorkOptions = new DbContextOptionsBuilder<SqlServerApplicationDbContext>()
                .UseSqlServer(
                        connectionString,
                        b => b.MigrationsAssembly("DevInstance.DevCoreApp.Server.Database.SqlServer"))
                .Options;
        services.AddScoped<IAppDbContextFactory>(sp =>
                new SqlServerAppDbContextFactory(unitOfWorkOptions, sp.GetRequiredService<IOperationContext>()));
        services.AddScoped<IQueryRepositoryFactory, QueryRepositoryFactory>();
    }

    public static void ConfigureSqlServerIdentityContext(this IServiceCollection services)
    {
        services.ConfigureIdentityContext<SqlServerApplicationDbContext>();
    }
}
