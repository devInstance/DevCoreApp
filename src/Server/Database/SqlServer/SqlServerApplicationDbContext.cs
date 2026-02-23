using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace DevInstance.DevCoreApp.Server.Database.SqlServer;

internal class SqlServerApplicationDbContext : ApplicationDbContext
{
    public SqlServerApplicationDbContext(DbContextOptions options, IOperationContext operationContext)
        : base(options, operationContext)
    {
    }
}
