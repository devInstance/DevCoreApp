using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public class CoreSettingsQuery : CoreBaseQuery<Setting, CoreSettingsQuery>, ISettingsQuery
{
    private CoreSettingsQuery(IQueryable<Setting> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreSettingsQuery(IScopeManager logManager,
                             ITimeProvider timeProvider,
                             ApplicationDbContext dB,
                             UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public ISettingsQuery ByPublicId(string id) => ByGuidIdHelper(id);

    public ISettingsQuery ByCategory(string category)
    {
        currentQuery = from s in currentQuery
                       where s.Category == category
                       select s;
        return this;
    }

    public ISettingsQuery ByCategoryAndKey(string category, string key)
    {
        currentQuery = from s in currentQuery
                       where s.Category == category && s.Key == key
                       select s;
        return this;
    }

    public ISettingsQuery ByTenantId(Guid? tenantId)
    {
        currentQuery = tenantId.HasValue
            ? (from s in currentQuery where s.TenantId == tenantId.Value select s)
            : (from s in currentQuery where s.TenantId == null select s);
        return this;
    }

    public ISettingsQuery ByOrganizationId(Guid? organizationId)
    {
        currentQuery = organizationId.HasValue
            ? (from s in currentQuery where s.OrganizationId == organizationId.Value select s)
            : (from s in currentQuery where s.OrganizationId == null select s);
        return this;
    }

    public ISettingsQuery ByUserId(Guid? userId)
    {
        currentQuery = userId.HasValue
            ? (from s in currentQuery where s.UserId == userId.Value select s)
            : (from s in currentQuery where s.UserId == null select s);
        return this;
    }

    public ISettingsQuery SystemOnly()
    {
        currentQuery = from s in currentQuery
                       where s.TenantId == null && s.OrganizationId == null && s.UserId == null
                       select s;
        return this;
    }

    public ISettingsQuery Clone()
    {
        return new CoreSettingsQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public Setting CreateNew()
    {
        return new Setting
        {
            Id = Guid.NewGuid(),
        };
    }

    public ISettingsQuery Search(string search)
    {
        currentQuery = from s in currentQuery
                       where s.Category.Contains(search) ||
                             s.Key.Contains(search) ||
                             (s.Description != null && s.Description.Contains(search))
                       select s;
        return this;
    }

    public ISettingsQuery Skip(int value) => SkipHelper(value);

    public ISettingsQuery Take(int value) => TakeHelper(value);

    public ISettingsQuery SortBy(string column, bool isAsc)
    {
        this.IsAsc = isAsc;

        if (string.Compare(column, "category", true) == 0)
        {
            currentQuery = isAsc
                ? (from s in currentQuery orderby s.Category select s)
                : (from s in currentQuery orderby s.Category descending select s);
            SortedBy = "category";
        }
        else if (string.Compare(column, "key", true) == 0)
        {
            currentQuery = isAsc
                ? (from s in currentQuery orderby s.Key select s)
                : (from s in currentQuery orderby s.Key descending select s);
            SortedBy = "key";
        }
        else
        {
            throw new ArgumentException("Invalid column name");
        }

        return this;
    }
}
