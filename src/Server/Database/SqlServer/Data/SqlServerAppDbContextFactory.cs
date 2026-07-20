using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace DevInstance.DevCoreApp.Server.Database.SqlServer.Data;

/// <summary>
/// Builds a fresh <see cref="SqlServerApplicationDbContext"/> from built-once options plus the
/// live scoped <see cref="IOperationContext"/>. Internal because <see cref="SqlServerApplicationDbContext"/>
/// is internal — a public factory constructor cannot expose an internal parameter type.
/// </summary>
internal sealed class SqlServerAppDbContextFactory : IAppDbContextFactory
{
    private readonly DbContextOptions<SqlServerApplicationDbContext> _options;
    private readonly IOperationContext _operationContext;

    public SqlServerAppDbContextFactory(DbContextOptions<SqlServerApplicationDbContext> options,
                                        IOperationContext operationContext)
    {
        _options = options;
        _operationContext = operationContext;
    }

    public ApplicationDbContext CreateDbContext()
        => new SqlServerApplicationDbContext(_options, _operationContext);
}
