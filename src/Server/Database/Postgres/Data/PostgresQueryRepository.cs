using DevInstance.LogScope;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Shared.Utils;

namespace DevInstance.DevCoreApp.Server.Database.Postgres.Data;

public class PostgresQueryRepository : CoreQueryRepository
{
    public PostgresQueryRepository(IScopeManager logManager,
                                    ITimeProvider timeProvider,
                                    ApplicationDbContext dB) 
        : base(logManager, timeProvider, dB)
    {
    }
}
