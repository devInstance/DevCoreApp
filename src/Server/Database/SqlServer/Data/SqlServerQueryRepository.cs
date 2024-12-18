using DevInstance.LogScope;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Postgres.Data;
using DevInstance.DevCoreApp.Shared.Utils;

namespace DevInstance.DevCoreApp.Server.Database.SqlServer.Data;

public class SqlServerQueryRepository : CoreQueryRepository
{
    public SqlServerQueryRepository(IScopeManager logManager,
                                    ITimeProvider timeProvider,
                                    ApplicationDbContext dB)
        : base(logManager, timeProvider, dB)
    {
    }
}
