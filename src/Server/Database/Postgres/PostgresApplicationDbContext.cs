using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace DevInstance.DevCoreApp.Server.Database.Postgres;

public class PostgresApplicationDbContext : ApplicationDbContext
{
    public PostgresApplicationDbContext(DbContextOptions options, IOperationContext operationContext)
        : base(options, operationContext)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("uuid-ossp");
    }
}
