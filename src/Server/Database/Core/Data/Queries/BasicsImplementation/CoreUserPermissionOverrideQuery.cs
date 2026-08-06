using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils.Core;
using DevInstance.LogScope;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public class CoreUserPermissionOverrideQuery : CoreBaseQuery<UserPermissionOverride, CoreUserPermissionOverrideQuery>, IUserPermissionOverrideQuery
{
    private CoreUserPermissionOverrideQuery(IQueryable<UserPermissionOverride> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreUserPermissionOverrideQuery(IScopeManager logManager,
                             ITimeProvider timeProvider,
                             ApplicationDbContext dB,
                             UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public IUserPermissionOverrideQuery ByUserId(Guid userId)
    {
        currentQuery = from upo in currentQuery
                       where upo.UserId == userId
                       select upo;
        return this;
    }

    public IUserPermissionOverrideQuery IncludePermission()
    {
        currentQuery = currentQuery.Include(upo => upo.Permission);
        return this;
    }

    public IUserPermissionOverrideQuery Clone()
    {
        return new CoreUserPermissionOverrideQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public UserPermissionOverride CreateNew()
    {
        return new UserPermissionOverride
        {
            Id = Guid.NewGuid(),
        };
    }

    public async Task ReplaceForUserAsync(Guid userId, IReadOnlyList<UserPermissionOverride> overrides)
    {
        var existing = await (from upo in DB.UserPermissionOverrides
                              where upo.UserId == userId
                              select upo).ToListAsync();
        DB.UserPermissionOverrides.RemoveRange(existing);

        if (overrides.Count > 0)
        {
            DB.UserPermissionOverrides.AddRange(overrides);
        }

        await DB.SaveChangesAsync();
    }
}
