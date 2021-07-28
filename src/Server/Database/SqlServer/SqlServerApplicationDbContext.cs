using DevInstance.SampleWebApp.Server.Database.Core;
using IdentityServer4.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevInstance.SampleWebApp.Server.Database.SqlServer
{
    public class SqlServerApplicationDbContext : ApplicationDbContext
    {
        public SqlServerApplicationDbContext(DbContextOptions options,
            IOptions<OperationalStoreOptions> operationalStoreOptions)
            : base(options, operationalStoreOptions)
        {
        }
    }
}
