using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Notifications;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public class CoreNotificationQuery : CoreBaseQuery<Notification, CoreNotificationQuery>, INotificationQuery
{
    private CoreNotificationQuery(IQueryable<Notification> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreNotificationQuery(IScopeManager logManager,
                             ITimeProvider timeProvider,
                             ApplicationDbContext dB,
                             UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public INotificationQuery ByPublicId(string id) => ByGuidIdHelper(id);

    public INotificationQuery ByUserProfileId(Guid userProfileId)
    {
        currentQuery = from n in currentQuery
                       where n.UserProfileId == userProfileId
                       select n;
        return this;
    }

    public INotificationQuery ByIsRead(bool isRead)
    {
        currentQuery = from n in currentQuery
                       where n.IsRead == isRead
                       select n;
        return this;
    }

    public INotificationQuery ByGroupKey(string groupKey)
    {
        currentQuery = from n in currentQuery
                       where n.GroupKey == groupKey
                       select n;
        return this;
    }

    public Notification CreateNew()
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            CreateDate = TimeProvider.CurrentTime,
        };
    }

    public INotificationQuery Clone()
    {
        return new CoreNotificationQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public INotificationQuery Search(string search)
    {
        currentQuery = from n in currentQuery
                       where n.Title.IndexOf(search) >= 0 ||
                             n.Message.IndexOf(search) >= 0
                       select n;
        return this;
    }

    public INotificationQuery Skip(int value) => SkipHelper(value);

    public INotificationQuery Take(int value) => TakeHelper(value);

    public INotificationQuery SortBy(string column, bool isAsc)
    {
        this.IsAsc = isAsc;

        if (string.Compare(column, "createdate", true) == 0)
        {
            currentQuery = isAsc
                ? (from n in currentQuery orderby n.CreateDate select n)
                : (from n in currentQuery orderby n.CreateDate descending select n);
            SortedBy = "createdate";
        }
        else if (string.Compare(column, "title", true) == 0)
        {
            currentQuery = isAsc
                ? (from n in currentQuery orderby n.Title select n)
                : (from n in currentQuery orderby n.Title descending select n);
            SortedBy = "title";
        }
        else if (string.Compare(column, "type", true) == 0)
        {
            currentQuery = isAsc
                ? (from n in currentQuery orderby n.Type select n)
                : (from n in currentQuery orderby n.Type descending select n);
            SortedBy = "type";
        }
        else if (string.Compare(column, "isread", true) == 0)
        {
            currentQuery = isAsc
                ? (from n in currentQuery orderby n.IsRead select n)
                : (from n in currentQuery orderby n.IsRead descending select n);
            SortedBy = "isread";
        }
        else
        {
            throw new ArgumentException("Invalid column name");
        }

        return this;
    }
}
