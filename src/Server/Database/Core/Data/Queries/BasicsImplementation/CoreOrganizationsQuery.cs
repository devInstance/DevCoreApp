using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using System;
using System.Linq;

namespace NoCrast.Server.Database.Postgres.Data.Queries;

public class CoreOrganizationsQuery : CoreDatabaseObjectQuery<Organization, CoreOrganizationsQuery>, IOrganizationsQuery
{
    private CoreOrganizationsQuery(IQueryable<Organization> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreOrganizationsQuery(IScopeManager logManager,
                                     ITimeProvider timeProvider,
                                     ApplicationDbContext dB,
                                     UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public IOrganizationsQuery ByPublicId(string id) => ByPublicIdHelper(id);

    public IOrganizationsQuery ByParentId(Guid? parentId)
    {
        currentQuery = from o in currentQuery
                       where o.ParentId == parentId
                       select o;
        return this;
    }

    public IOrganizationsQuery ByPathPrefix(string pathPrefix)
    {
        currentQuery = from o in currentQuery
                       where o.Path.StartsWith(pathPrefix)
                       select o;
        return this;
    }

    public IOrganizationsQuery ByType(string type)
    {
        currentQuery = from o in currentQuery
                       where o.Type == type
                       select o;
        return this;
    }

    public IOrganizationsQuery ByIsActive(bool isActive)
    {
        currentQuery = from o in currentQuery
                       where o.IsActive == isActive
                       select o;
        return this;
    }

    public IOrganizationsQuery ByLevel(int level)
    {
        currentQuery = from o in currentQuery
                       where o.Level == level
                       select o;
        return this;
    }

    public IOrganizationsQuery ByCode(string code)
    {
        currentQuery = from o in currentQuery
                       where o.Code == code
                       select o;
        return this;
    }

    public IOrganizationsQuery Clone()
    {
        return new CoreOrganizationsQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public IOrganizationsQuery Search(string search)
    {
        currentQuery = from o in currentQuery
                       where o.Name.IndexOf(search) >= 0 ||
                                o.Code.IndexOf(search) >= 0 ||
                                o.Path.IndexOf(search) >= 0
                       select o;
        return this;
    }

    public IOrganizationsQuery Skip(int value) => SkipHelper(value);

    public IOrganizationsQuery Take(int value) => TakeHelper(value);

    public IOrganizationsQuery SortBy(string column, bool isAsc)
    {
        this.IsAsc = isAsc;
        if (string.Compare(column, "Name", true) == 0)
        {
            if (isAsc)
            {
                currentQuery = (from o in currentQuery
                                orderby o.Name
                                select o);
            }
            else
            {
                currentQuery = (from o in currentQuery
                                orderby o.Name descending
                                select o);
            }
            SortedBy = "Name";
        }
        else if (string.Compare(column, "Code", true) == 0)
        {
            if (isAsc)
            {
                currentQuery = (from o in currentQuery
                                orderby o.Code
                                select o);
            }
            else
            {
                currentQuery = (from o in currentQuery
                                orderby o.Code descending
                                select o);
            }
            SortedBy = "Code";
        }
        else if (string.Compare(column, "Level", true) == 0)
        {
            if (isAsc)
            {
                currentQuery = (from o in currentQuery
                                orderby o.Level
                                select o);
            }
            else
            {
                currentQuery = (from o in currentQuery
                                orderby o.Level descending
                                select o);
            }
            SortedBy = "Level";
        }
        else if (string.Compare(column, "Path", true) == 0)
        {
            if (isAsc)
            {
                currentQuery = (from o in currentQuery
                                orderby o.Path
                                select o);
            }
            else
            {
                currentQuery = (from o in currentQuery
                                orderby o.Path descending
                                select o);
            }
            SortedBy = "Path";
        }
        else if (string.Compare(column, "Type", true) == 0)
        {
            if (isAsc)
            {
                currentQuery = (from o in currentQuery
                                orderby o.Type
                                select o);
            }
            else
            {
                currentQuery = (from o in currentQuery
                                orderby o.Type descending
                                select o);
            }
            SortedBy = "Type";
        }
        else if (string.Compare(column, "SortOrder", true) == 0)
        {
            if (isAsc)
            {
                currentQuery = (from o in currentQuery
                                orderby o.SortOrder
                                select o);
            }
            else
            {
                currentQuery = (from o in currentQuery
                                orderby o.SortOrder descending
                                select o);
            }
            SortedBy = "SortOrder";
        }
        else
        {
            throw new ArgumentException("Invalid column name");
        }

        return this;
    }
}
