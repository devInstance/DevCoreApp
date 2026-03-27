using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public class CoreEmailLogQuery : CoreDatabaseObjectQuery<EmailLog, CoreEmailLogQuery>, IEmailLogQuery
{
    private CoreEmailLogQuery(IQueryable<EmailLog> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreEmailLogQuery(IScopeManager logManager,
                             ITimeProvider timeProvider,
                             ApplicationDbContext dB,
                             UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public IEmailLogQuery ByPublicId(string id) => ByPublicIdHelper(id);

    public IEmailLogQuery ByStatus(EmailLogStatus status)
    {
        currentQuery = from el in currentQuery
                       where el.Status == status
                       select el;
        return this;
    }

    public IEmailLogQuery ByTemplateName(string templateName)
    {
        currentQuery = from el in currentQuery
                       where el.TemplateName == templateName
                       select el;
        return this;
    }

    public IEmailLogQuery ByDateRange(DateTime? start, DateTime? end)
    {
        if (start.HasValue)
        {
            currentQuery = from el in currentQuery
                           where el.ScheduledDate >= start.Value
                           select el;
        }
        if (end.HasValue)
        {
            currentQuery = from el in currentQuery
                           where el.ScheduledDate <= end.Value
                           select el;
        }
        return this;
    }

    public IEmailLogQuery Clone()
    {
        return new CoreEmailLogQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public IEmailLogQuery Search(string search)
    {
        currentQuery = from el in currentQuery
                       where el.Subject.IndexOf(search) >= 0 ||
                             el.ToAddress.IndexOf(search) >= 0 ||
                             el.FromAddress.IndexOf(search) >= 0
                       select el;
        return this;
    }

    public IEmailLogQuery Skip(int value) => SkipHelper(value);

    public IEmailLogQuery Take(int value) => TakeHelper(value);

    public IEmailLogQuery SortBy(string column, bool isAsc)
    {
        this.IsAsc = isAsc;

        if (string.Compare(column, "scheduleddate", true) == 0)
        {
            currentQuery = isAsc
                ? (from el in currentQuery orderby el.ScheduledDate select el)
                : (from el in currentQuery orderby el.ScheduledDate descending select el);
            SortedBy = "scheduleddate";
        }
        else if (string.Compare(column, "sentdate", true) == 0)
        {
            currentQuery = isAsc
                ? (from el in currentQuery orderby el.SentDate select el)
                : (from el in currentQuery orderby el.SentDate descending select el);
            SortedBy = "sentdate";
        }
        else if (string.Compare(column, "subject", true) == 0)
        {
            currentQuery = isAsc
                ? (from el in currentQuery orderby el.Subject select el)
                : (from el in currentQuery orderby el.Subject descending select el);
            SortedBy = "subject";
        }
        else if (string.Compare(column, "toaddress", true) == 0)
        {
            currentQuery = isAsc
                ? (from el in currentQuery orderby el.ToAddress select el)
                : (from el in currentQuery orderby el.ToAddress descending select el);
            SortedBy = "toaddress";
        }
        else if (string.Compare(column, "fromaddress", true) == 0)
        {
            currentQuery = isAsc
                ? (from el in currentQuery orderby el.FromAddress select el)
                : (from el in currentQuery orderby el.FromAddress descending select el);
            SortedBy = "fromaddress";
        }
        else if (string.Compare(column, "status", true) == 0)
        {
            currentQuery = isAsc
                ? (from el in currentQuery orderby el.Status select el)
                : (from el in currentQuery orderby el.Status descending select el);
            SortedBy = "status";
        }
        else if (string.Compare(column, "templatename", true) == 0)
        {
            currentQuery = isAsc
                ? (from el in currentQuery orderby el.TemplateName select el)
                : (from el in currentQuery orderby el.TemplateName descending select el);
            SortedBy = "templatename";
        }
        else if (string.Compare(column, "providermessageid", true) == 0)
        {
            currentQuery = isAsc
                ? (from el in currentQuery orderby el.ProviderMessageId select el)
                : (from el in currentQuery orderby el.ProviderMessageId descending select el);
            SortedBy = "providermessageid";
        }
        else if (string.Compare(column, "openeddate", true) == 0)
        {
            currentQuery = isAsc
                ? (from el in currentQuery orderby el.OpenedDate select el)
                : (from el in currentQuery orderby el.OpenedDate descending select el);
            SortedBy = "openeddate";
        }
        else
        {
            throw new ArgumentException("Invalid column name");
        }

        return this;
    }
}
