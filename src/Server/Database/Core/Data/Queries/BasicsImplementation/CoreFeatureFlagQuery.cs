using System;
using System.Linq;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public class CoreFeatureFlagQuery : CoreDatabaseObjectQuery<FeatureFlag, CoreFeatureFlagQuery>, IFeatureFlagQuery
{
    private CoreFeatureFlagQuery(IQueryable<FeatureFlag> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreFeatureFlagQuery(IScopeManager logManager,
                                ITimeProvider timeProvider,
                                ApplicationDbContext dB,
                                UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public IFeatureFlagQuery ByPublicId(string id) => ByPublicIdHelper(id);

    public IFeatureFlagQuery ByName(string name)
    {
        currentQuery = from ff in currentQuery
                       where ff.Name == name
                       select ff;
        return this;
    }

    public IFeatureFlagQuery ByOrganizationId(Guid organizationId)
    {
        currentQuery = from ff in currentQuery
                       where ff.OrganizationId == organizationId
                       select ff;
        return this;
    }

    public IFeatureFlagQuery GlobalOnly()
    {
        currentQuery = from ff in currentQuery
                       where ff.OrganizationId == null
                       select ff;
        return this;
    }

    public IFeatureFlagQuery ByNameForEvaluation(string name)
    {
        currentQuery = from ff in currentQuery
                       where ff.Name == name
                       select ff;
        return this;
    }

    public IFeatureFlagQuery Clone()
    {
        return new CoreFeatureFlagQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public IFeatureFlagQuery Search(string search)
    {
        currentQuery = from ff in currentQuery
                       where ff.Name.Contains(search) ||
                             (ff.Description != null && ff.Description.Contains(search))
                       select ff;
        return this;
    }

    public IFeatureFlagQuery Skip(int value) => SkipHelper(value);

    public IFeatureFlagQuery Take(int value) => TakeHelper(value);

    public IFeatureFlagQuery SortBy(string column, bool isAsc)
    {
        this.IsAsc = isAsc;

        if (string.Compare(column, "name", true) == 0)
        {
            currentQuery = isAsc
                ? (from ff in currentQuery orderby ff.Name select ff)
                : (from ff in currentQuery orderby ff.Name descending select ff);
            SortedBy = "name";
        }
        else if (string.Compare(column, "isenabled", true) == 0)
        {
            currentQuery = isAsc
                ? (from ff in currentQuery orderby ff.IsEnabled select ff)
                : (from ff in currentQuery orderby ff.IsEnabled descending select ff);
            SortedBy = "isenabled";
        }
        else if (string.Compare(column, "createdate", true) == 0)
        {
            currentQuery = isAsc
                ? (from ff in currentQuery orderby ff.CreateDate select ff)
                : (from ff in currentQuery orderby ff.CreateDate descending select ff);
            SortedBy = "createdate";
        }
        else
        {
            throw new ArgumentException("Invalid column name");
        }

        return this;
    }
}
