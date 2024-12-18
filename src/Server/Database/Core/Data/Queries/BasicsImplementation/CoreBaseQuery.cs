using DevInstance.LogScope;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;

namespace NoCrast.Server.Database.Postgres.Data.Queries;

public class CoreBaseQuery
{
    public ITimeProvider TimeProvider { get; }

    public IScopeManager LogManager { get; }
    public IScopeLog Log { get; }

    protected ApplicationDbContext DB { get; }

    protected UserProfile CurrentProfile { get; }

    public CoreBaseQuery(IScopeManager logManager,
                                ITimeProvider timeProvider,
                                ApplicationDbContext dB,
                                UserProfile currentProfile)
    {
        LogManager = logManager;
        Log = logManager.CreateLogger(this);

        TimeProvider = timeProvider;
        DB = dB;

        CurrentProfile = currentProfile;
    }

}
