using System;
using System.Linq;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Webhooks;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public class CoreWebhookSubscriptionQuery : CoreDatabaseObjectQuery<WebhookSubscription, CoreWebhookSubscriptionQuery>, IWebhookSubscriptionQuery
{
    private CoreWebhookSubscriptionQuery(IQueryable<WebhookSubscription> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreWebhookSubscriptionQuery(IScopeManager logManager,
                                         ITimeProvider timeProvider,
                                         ApplicationDbContext dB,
                                         UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public IWebhookSubscriptionQuery ByPublicId(string id) => ByPublicIdHelper(id);

    public IWebhookSubscriptionQuery ByEventType(string eventType)
    {
        currentQuery = from ws in currentQuery
                       where ws.EventType == eventType
                       select ws;
        return this;
    }

    public IWebhookSubscriptionQuery ActiveOnly()
    {
        currentQuery = from ws in currentQuery
                       where ws.IsActive
                       select ws;
        return this;
    }

    public IWebhookSubscriptionQuery Clone()
    {
        return new CoreWebhookSubscriptionQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public IWebhookSubscriptionQuery Search(string search)
    {
        currentQuery = from ws in currentQuery
                       where ws.EventType.Contains(search) ||
                             ws.Url.Contains(search)
                       select ws;
        return this;
    }

    public IWebhookSubscriptionQuery Skip(int value) => SkipHelper(value);

    public IWebhookSubscriptionQuery Take(int value) => TakeHelper(value);

    public IWebhookSubscriptionQuery SortBy(string column, bool isAsc)
    {
        this.IsAsc = isAsc;

        if (string.Compare(column, "eventtype", true) == 0)
        {
            currentQuery = isAsc
                ? (from ws in currentQuery orderby ws.EventType select ws)
                : (from ws in currentQuery orderby ws.EventType descending select ws);
            SortedBy = "eventtype";
        }
        else if (string.Compare(column, "url", true) == 0)
        {
            currentQuery = isAsc
                ? (from ws in currentQuery orderby ws.Url select ws)
                : (from ws in currentQuery orderby ws.Url descending select ws);
            SortedBy = "url";
        }
        else if (string.Compare(column, "createdate", true) == 0)
        {
            currentQuery = isAsc
                ? (from ws in currentQuery orderby ws.CreateDate select ws)
                : (from ws in currentQuery orderby ws.CreateDate descending select ws);
            SortedBy = "createdate";
        }
        else
        {
            throw new ArgumentException("Invalid column name");
        }

        return this;
    }
}
