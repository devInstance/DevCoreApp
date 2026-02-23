using DevInstance.DevCoreApp.Server.Database.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Server.Database.Postgres;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PostgresApplicationDbContext>
{
    public PostgresApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PostgresApplicationDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=devcore_design;Username=postgres;Password=postgres",
            b => b.MigrationsAssembly("DevInstance.DevCoreApp.Server.Database.Postgres"));

        return new PostgresApplicationDbContext(optionsBuilder.Options, new NullOperationContext());
    }

    private class NullOperationContext : IOperationContext
    {
        public Guid? UserId => null;
        public Guid? PrimaryOrganizationId => null;
        public IReadOnlySet<Guid> VisibleOrganizationIds => new HashSet<Guid>();
        public string? IpAddress => null;
        public string? CorrelationId => null;
    }
}
