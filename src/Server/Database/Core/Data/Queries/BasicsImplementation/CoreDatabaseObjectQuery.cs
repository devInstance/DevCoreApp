using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Common.Tools;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NoCrast.Server.Database.Postgres.Data.Queries;

public class CoreDatabaseObjectQuery<TEntity, TSelf> : CoreBaseQuery<TEntity, TSelf>
    where TEntity : DatabaseObject, new()
    where TSelf : CoreDatabaseObjectQuery<TEntity, TSelf>
{
    protected CoreDatabaseObjectQuery(IQueryable<TEntity> query,
                                      IScopeManager logManager,
                                      ITimeProvider timeProvider,
                                      ApplicationDbContext dB,
                                      UserProfile currentProfile)
        : base(query, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreDatabaseObjectQuery(IScopeManager logManager,
                                   ITimeProvider timeProvider,
                                   ApplicationDbContext dB,
                                   UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public TEntity CreateNew()
    {
        DateTime now = TimeProvider.CurrentTime;

        return new TEntity
        {
            Id = Guid.NewGuid(),
            PublicId = IdGenerator.New(),
            CreateDate = now,
            UpdateDate = now,
        };
    }

    public override async Task UpdateAsync(TEntity record)
    {
        DateTime now = TimeProvider.CurrentTime;
        record.UpdateDate = now;
        DB.Set<TEntity>().Update(record);
        await DB.SaveChangesAsync();
    }

    protected TSelf ByPublicIdHelper(string id)
    {
        currentQuery = from e in currentQuery
                       where e.PublicId == id
                       select e;
        return (TSelf)this;
    }
}
