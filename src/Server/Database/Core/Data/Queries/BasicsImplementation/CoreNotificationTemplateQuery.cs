using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Notifications;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using System;
using System.Linq;

namespace NoCrast.Server.Database.Postgres.Data.Queries;

public class CoreNotificationTemplateQuery : CoreDatabaseObjectQuery<NotificationTemplate, CoreNotificationTemplateQuery>, INotificationTemplateQuery
{
    private CoreNotificationTemplateQuery(IQueryable<NotificationTemplate> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreNotificationTemplateQuery(IScopeManager logManager,
                             ITimeProvider timeProvider,
                             ApplicationDbContext dB,
                             UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public INotificationTemplateQuery ByPublicId(string id) => ByPublicIdHelper(id);

    public INotificationTemplateQuery ByCategory(string category)
    {
        currentQuery = from nt in currentQuery
                       where nt.Category == category
                       select nt;
        return this;
    }

    public INotificationTemplateQuery ByName(string name)
    {
        currentQuery = from nt in currentQuery
                       where nt.Name == name
                       select nt;
        return this;
    }

    public INotificationTemplateQuery Clone()
    {
        return new CoreNotificationTemplateQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public INotificationTemplateQuery Search(string search)
    {
        currentQuery = from nt in currentQuery
                       where nt.Name.IndexOf(search) >= 0 ||
                             nt.TitleTemplate.IndexOf(search) >= 0 ||
                             nt.Category.IndexOf(search) >= 0
                       select nt;
        return this;
    }

    public INotificationTemplateQuery Skip(int value) => SkipHelper(value);

    public INotificationTemplateQuery Take(int value) => TakeHelper(value);

    public INotificationTemplateQuery SortBy(string column, bool isAsc)
    {
        this.IsAsc = isAsc;

        if (string.Compare(column, "name", true) == 0)
        {
            currentQuery = isAsc
                ? (from nt in currentQuery orderby nt.Name select nt)
                : (from nt in currentQuery orderby nt.Name descending select nt);
            SortedBy = "name";
        }
        else if (string.Compare(column, "category", true) == 0)
        {
            currentQuery = isAsc
                ? (from nt in currentQuery orderby nt.Category select nt)
                : (from nt in currentQuery orderby nt.Category descending select nt);
            SortedBy = "category";
        }
        else if (string.Compare(column, "defaulttype", true) == 0)
        {
            currentQuery = isAsc
                ? (from nt in currentQuery orderby nt.DefaultType select nt)
                : (from nt in currentQuery orderby nt.DefaultType descending select nt);
            SortedBy = "defaulttype";
        }
        else if (string.Compare(column, "createdate", true) == 0)
        {
            currentQuery = isAsc
                ? (from nt in currentQuery orderby nt.CreateDate select nt)
                : (from nt in currentQuery orderby nt.CreateDate descending select nt);
            SortedBy = "createdate";
        }
        else
        {
            throw new ArgumentException("Invalid column name");
        }

        return this;
    }
}
