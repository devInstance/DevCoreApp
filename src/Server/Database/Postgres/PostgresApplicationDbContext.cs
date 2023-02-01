using DevInstance.DevCoreApp.Server.Database.Core;
using Microsoft.EntityFrameworkCore;

namespace DevInstance.DevCoreApp.Server.Database.Postgres
{
    internal class PostgresApplicationDbContext : ApplicationDbContext
    {
        public PostgresApplicationDbContext(DbContextOptions options)
            : base(options)
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
}
