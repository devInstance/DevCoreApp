using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public class CoreBaseQuery<TEntity, TSelf> : CoreBaseQuery
    where TEntity : DatabaseBaseObject
    where TSelf : CoreBaseQuery<TEntity, TSelf>
{
    protected IQueryable<TEntity> currentQuery;

    public string SortedBy { get; set; }

    public bool IsAsc { get; set; }

    protected CoreBaseQuery(IQueryable<TEntity> query,
                            IScopeManager logManager,
                            ITimeProvider timeProvider,
                            ApplicationDbContext dB,
                            UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
        currentQuery = query;
    }

    public CoreBaseQuery(IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : this(dB.Set<TEntity>(), logManager, timeProvider, dB, currentProfile)
    {
    }

    public IQueryable<TEntity> Select()
    {
        return currentQuery;
    }

    public async Task AddAsync(TEntity record)
    {
        DB.Set<TEntity>().Add(record);
        await DB.SaveChangesAsync();
    }

    public async Task RemoveAsync(TEntity record)
    {
        DB.Set<TEntity>().Remove(record);
        await DB.SaveChangesAsync();
    }

    public virtual async Task UpdateAsync(TEntity record)
    {
        DB.Set<TEntity>().Update(record);
        await DB.SaveChangesAsync();
    }

    protected TSelf SkipHelper(int value)
    {
        currentQuery = currentQuery.Skip(value);
        return (TSelf)this;
    }

    protected TSelf TakeHelper(int value)
    {
        currentQuery = currentQuery.Take(value);
        return (TSelf)this;
    }

    protected TSelf ByGuidIdHelper(string id)
    {
        if (Guid.TryParse(id, out var guid))
        {
            currentQuery = from e in currentQuery
                           where e.Id == guid
                           select e;
        }
        return (TSelf)this;
    }
}
