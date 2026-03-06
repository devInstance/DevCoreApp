using System;
using System.Linq;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Webhooks;
using DevInstance.DevCoreApp.Shared.Model.Webhooks;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;

namespace NoCrast.Server.Database.Postgres.Data.Queries;

public class CoreWebhookDeliveryQuery : CoreDatabaseObjectQuery<WebhookDelivery, CoreWebhookDeliveryQuery>, IWebhookDeliveryQuery
{
    private CoreWebhookDeliveryQuery(IQueryable<WebhookDelivery> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreWebhookDeliveryQuery(IScopeManager logManager,
                                     ITimeProvider timeProvider,
                                     ApplicationDbContext dB,
                                     UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public IWebhookDeliveryQuery ByPublicId(string id) => ByPublicIdHelper(id);

    public IWebhookDeliveryQuery BySubscriptionId(Guid subscriptionId)
    {
        currentQuery = from wd in currentQuery
                       where wd.SubscriptionId == subscriptionId
                       select wd;
        return this;
    }

    public IWebhookDeliveryQuery ByStatus(WebhookDeliveryStatus status)
    {
        currentQuery = from wd in currentQuery
                       where wd.Status == status
                       select wd;
        return this;
    }

    public IWebhookDeliveryQuery ByEventType(string eventType)
    {
        currentQuery = from wd in currentQuery
                       where wd.EventType == eventType
                       select wd;
        return this;
    }

    public IWebhookDeliveryQuery Clone()
    {
        return new CoreWebhookDeliveryQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public IWebhookDeliveryQuery Skip(int value) => SkipHelper(value);

    public IWebhookDeliveryQuery Take(int value) => TakeHelper(value);

    public IWebhookDeliveryQuery SortBy(string column, bool isAsc)
    {
        this.IsAsc = isAsc;

        if (string.Compare(column, "eventtype", true) == 0)
        {
            currentQuery = isAsc
                ? (from wd in currentQuery orderby wd.EventType select wd)
                : (from wd in currentQuery orderby wd.EventType descending select wd);
            SortedBy = "eventtype";
        }
        else if (string.Compare(column, "status", true) == 0)
        {
            currentQuery = isAsc
                ? (from wd in currentQuery orderby wd.Status select wd)
                : (from wd in currentQuery orderby wd.Status descending select wd);
            SortedBy = "status";
        }
        else if (string.Compare(column, "createdate", true) == 0)
        {
            currentQuery = isAsc
                ? (from wd in currentQuery orderby wd.CreateDate select wd)
                : (from wd in currentQuery orderby wd.CreateDate descending select wd);
            SortedBy = "createdate";
        }
        else
        {
            throw new ArgumentException("Invalid column name");
        }

        return this;
    }
}
