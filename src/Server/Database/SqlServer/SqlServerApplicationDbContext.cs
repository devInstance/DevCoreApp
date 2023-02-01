using DevInstance.DevCoreApp.Server.Database.Core;
using Microsoft.EntityFrameworkCore;

namespace DevInstance.DevCoreApp.Server.Database.SqlServer
{
    internal class SqlServerApplicationDbContext : ApplicationDbContext
    {
        public SqlServerApplicationDbContext(DbContextOptions options)
            : base(options)
        {
        }
    }
}
