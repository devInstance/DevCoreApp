using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Common.Tools;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

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

        var entity = new TEntity
        {
            Id = Guid.NewGuid(),
            PublicId = IdGenerator.New(),
            CreateDate = now,
            UpdateDate = now,
        };

        StampCreatedBy(entity);

        return entity;
    }

    public override async Task UpdateAsync(TEntity record)
    {
        DateTime now = TimeProvider.CurrentTime;
        record.UpdateDate = now;
        StampUpdatedBy(record);
        DB.Set<TEntity>().Update(record);
        await DB.SaveChangesAsync();
    }

    // Set the scalar FKs, NEVER the CreatedBy/UpdatedBy navigations. Each service opens its own
    // short-lived context (per-operation unit of work), and CurrentProfile was materialized by a
    // DIFFERENT context — see AuthorizationContext, which resolves it through the DI-scoped
    // IQueryRepository. Assigning the navigation would pull that untracked UserProfile into this
    // context's Added graph, so SaveChanges would try to INSERT the user and fail with a
    // duplicate key ("An error occurred while saving the entity changes").
    private void StampCreatedBy(TEntity entity)
    {
        if (entity is DatabaseEntityObject entityObject)
        {
            entityObject.CreatedById = CurrentProfile?.Id;
            entityObject.UpdatedById = CurrentProfile?.Id;
        }
    }

    private void StampUpdatedBy(TEntity entity)
    {
        if (entity is DatabaseEntityObject entityObject && CurrentProfile != null)
        {
            entityObject.UpdatedById = CurrentProfile.Id;
        }
    }

    protected TSelf ByPublicIdHelper(string id)
    {
        currentQuery = from e in currentQuery
                       where e.PublicId == id
                       select e;
        return (TSelf)this;
    }
}
