using DevInstance.BlazorToolkit.Utils;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NoCrast.Server.Database.Postgres.Data.Queries;

public class CoreGridProfilesQuery : CoreBaseQuery, IGridProfilesQuery
{
    private IQueryable<GridProfile> currentQuery;

    private CoreGridProfilesQuery(IQueryable<GridProfile> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
        currentQuery = q;
    }

    public CoreGridProfilesQuery(IScopeManager logManager,
                                 ITimeProvider timeProvider,
                                 ApplicationDbContext dB,
                                 UserProfile currentProfile)
        : this(from gp in dB.GridProfiles select gp, logManager, timeProvider, dB, currentProfile)
    {
    }

    public async Task AddAsync(GridProfile record)
    {
        DB.GridProfiles.Add(record);
        await DB.SaveChangesAsync();
    }

    public IGridProfilesQuery ByPublicId(string id)
    {
        currentQuery = from gp in currentQuery
                       where gp.PublicId == id
                       select gp;
        return this;
    }

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

    public GridProfile CreateNew()
    {
        DateTime now = TimeProvider.CurrentTime;

        return new GridProfile
        {
            Id = Guid.NewGuid(),
            PublicId = IdGenerator.New(),
            CreateDate = now,
            UpdateDate = now,
        };
    }

    public async Task RemoveAsync(GridProfile record)
    {
        DB.GridProfiles.Remove(record);
        await DB.SaveChangesAsync();
    }

    public IQueryable<GridProfile> Select()
    {
        return currentQuery;
    }

    public async Task UpdateAsync(GridProfile record)
    {
        DateTime now = TimeProvider.CurrentTime;
        record.UpdateDate = now;
        DB.GridProfiles.Update(record);
        await DB.SaveChangesAsync();
    }
}
