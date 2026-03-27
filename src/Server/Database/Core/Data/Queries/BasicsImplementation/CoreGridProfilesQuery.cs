using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public class CoreGridProfilesQuery : CoreDatabaseObjectQuery<GridProfile, CoreGridProfilesQuery>, IGridProfilesQuery
{
    private CoreGridProfilesQuery(IQueryable<GridProfile> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreGridProfilesQuery(IScopeManager logManager,
                                 ITimeProvider timeProvider,
                                 ApplicationDbContext dB,
                                 UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public IGridProfilesQuery ByPublicId(string id) => ByPublicIdHelper(id);

    public IGridProfilesQuery ByUserProfileId(Guid userProfileId)
    {
        currentQuery = from gp in currentQuery
                       where gp.UserProfileId == userProfileId
                       select gp;
        return this;
    }

    public IGridProfilesQuery ByGridName(string gridName)
    {
        currentQuery = from gp in currentQuery
                       where gp.GridName == gridName
                       select gp;
        return this;
    }

    public IGridProfilesQuery ByProfileName(string profileName)
    {
        currentQuery = from gp in currentQuery
                       where gp.ProfileName == profileName
                       select gp;
        return this;
    }

    public IGridProfilesQuery ByIsGlobal(bool isGlobal)
    {
        currentQuery = from gp in currentQuery
                       where gp.IsGlobal == isGlobal
                       select gp;
        return this;
    }

    public IGridProfilesQuery Clone()
    {
        return new CoreGridProfilesQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }
}
