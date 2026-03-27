using System;
using System.Linq;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public class CoreApiKeyQuery : CoreDatabaseObjectQuery<ApiKey, CoreApiKeyQuery>, IApiKeyQuery
{
    private CoreApiKeyQuery(IQueryable<ApiKey> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreApiKeyQuery(IScopeManager logManager,
                            ITimeProvider timeProvider,
                            ApplicationDbContext dB,
                            UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public IApiKeyQuery ByPublicId(string id) => ByPublicIdHelper(id);

    public IApiKeyQuery ByKeyHash(string keyHash)
    {
        currentQuery = from ak in currentQuery
                       where ak.KeyHash == keyHash
                       select ak;
        return this;
    }

    public IApiKeyQuery ByCreatedById(Guid userId)
    {
        currentQuery = from ak in currentQuery
                       where ak.CreatedById == userId
                       select ak;
        return this;
    }

    public IApiKeyQuery ActiveOnly()
    {
        currentQuery = from ak in currentQuery
                       where ak.IsActive && !ak.IsRevoked
                       select ak;
        return this;
    }

    public IApiKeyQuery Clone()
    {
        return new CoreApiKeyQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public IApiKeyQuery Search(string search)
    {
        currentQuery = from ak in currentQuery
                       where ak.Name.Contains(search) ||
                             ak.Prefix.Contains(search)
                       select ak;
        return this;
    }

    public IApiKeyQuery Skip(int value) => SkipHelper(value);

    public IApiKeyQuery Take(int value) => TakeHelper(value);

    public IApiKeyQuery SortBy(string column, bool isAsc)
    {
        this.IsAsc = isAsc;

        if (string.Compare(column, "name", true) == 0)
        {
            currentQuery = isAsc
                ? (from ak in currentQuery orderby ak.Name select ak)
                : (from ak in currentQuery orderby ak.Name descending select ak);
            SortedBy = "name";
        }
        else if (string.Compare(column, "expiresat", true) == 0)
        {
            currentQuery = isAsc
                ? (from ak in currentQuery orderby ak.ExpiresAt select ak)
                : (from ak in currentQuery orderby ak.ExpiresAt descending select ak);
            SortedBy = "expiresat";
        }
        else if (string.Compare(column, "usedat", true) == 0)
        {
            currentQuery = isAsc
                ? (from ak in currentQuery orderby ak.LastUsedAt select ak)
                : (from ak in currentQuery orderby ak.LastUsedAt descending select ak);
            SortedBy = "usedat";
        }
        else if (string.Compare(column, "createdate", true) == 0)
        {
            currentQuery = isAsc
                ? (from ak in currentQuery orderby ak.CreateDate select ak)
                : (from ak in currentQuery orderby ak.CreateDate descending select ak);
            SortedBy = "createdate";
        }
        else
        {
            throw new ArgumentException("Invalid column name");
        }

        return this;
    }
}
