using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public class CoreUserOrganizationQuery : CoreBaseQuery<UserOrganization, CoreUserOrganizationQuery>, IUserOrganizationQuery
{
    private CoreUserOrganizationQuery(IQueryable<UserOrganization> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreUserOrganizationQuery(IScopeManager logManager,
                             ITimeProvider timeProvider,
                             ApplicationDbContext dB,
                             UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public IUserOrganizationQuery ByUserId(Guid userId)
    {
        currentQuery = from uo in currentQuery
                       where uo.UserId == userId
                       select uo;
        return this;
    }

    public IUserOrganizationQuery IncludeOrganization()
    {
        currentQuery = currentQuery.Include(uo => uo.Organization);
        return this;
    }

    public IUserOrganizationQuery Clone()
    {
        return new CoreUserOrganizationQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public UserOrganization CreateNew()
    {
        return new UserOrganization
        {
            Id = Guid.NewGuid(),
        };
    }

    public async Task ReplaceForUserAsync(Guid userId, IReadOnlyList<UserOrganization> assignments)
    {
        var existing = await (from uo in DB.UserOrganizations
                              where uo.UserId == userId
                              select uo).ToListAsync();
        DB.UserOrganizations.RemoveRange(existing);

        if (assignments.Count > 0)
        {
            DB.UserOrganizations.AddRange(assignments);
        }

        await DB.SaveChangesAsync();
    }
}
