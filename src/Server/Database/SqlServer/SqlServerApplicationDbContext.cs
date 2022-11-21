using DevInstance.DevCoreApp.Server.Database.Core;
using IdentityServer4.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevInstance.DevCoreApp.Server.Database.SqlServer
{
    internal class SqlServerApplicationDbContext : ApplicationDbContext
    {
        public SqlServerApplicationDbContext(DbContextOptions options
            /*,IOptions<OperationalStoreOptions> operationalStoreOptions*/)
            : base(options/*, operationalStoreOptions*/)
        {
        }
    }
}
