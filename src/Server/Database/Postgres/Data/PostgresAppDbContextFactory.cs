using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace DevInstance.DevCoreApp.Server.Database.Postgres.Data;

/// <summary>
/// Builds a fresh <see cref="PostgresApplicationDbContext"/> from built-once options plus the
/// live scoped <see cref="IOperationContext"/>. Registered scoped so each created context binds
/// to the current scope's operation context (keeps audit + org query filters correct).
/// </summary>
public sealed class PostgresAppDbContextFactory : IAppDbContextFactory
{
    private readonly DbContextOptions<PostgresApplicationDbContext> _options;
    private readonly IOperationContext _operationContext;

    public PostgresAppDbContextFactory(DbContextOptions<PostgresApplicationDbContext> options,
                                       IOperationContext operationContext)
    {
        _options = options;
        _operationContext = operationContext;
    }

    public ApplicationDbContext CreateDbContext()
        => new PostgresApplicationDbContext(_options, _operationContext);
}
