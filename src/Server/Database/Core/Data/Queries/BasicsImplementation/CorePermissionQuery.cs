using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public class CorePermissionQuery : CoreBaseQuery<Permission, CorePermissionQuery>, IPermissionQuery
{
    private CorePermissionQuery(IQueryable<Permission> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CorePermissionQuery(IScopeManager logManager,
                             ITimeProvider timeProvider,
                             ApplicationDbContext dB,
                             UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public IPermissionQuery ByKeys(IEnumerable<string> keys)
    {
        currentQuery = from p in currentQuery
                       where keys.Contains(p.Key)
                       select p;
        return this;
    }

    public IPermissionQuery OrderedByDisplayOrder()
    {
        currentQuery = from p in currentQuery
                       orderby p.DisplayOrder
                       select p;
        return this;
    }

    public IPermissionQuery Clone()
    {
        return new CorePermissionQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public Permission CreateNew()
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
        };
    }
}
