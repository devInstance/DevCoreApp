using DevInstance.BlazorToolkit.Utils;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NoCrast.Server.Database.Postgres.Data.Queries;

public class CoreEmailLogQuery : CoreBaseQuery, IEmailLogQuery
{
    private IQueryable<EmailLog> currentQuery;

    public string SortedBy { get; set; }

    public bool IsAsc { get; set; }

    private CoreEmailLogQuery(IQueryable<EmailLog> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
        currentQuery = q;
    }

    public CoreEmailLogQuery(IScopeManager logManager,
                             ITimeProvider timeProvider,
                             ApplicationDbContext dB,
                             UserProfile currentProfile)
        : this(from el in dB.EmailLogs select el, logManager, timeProvider, dB, currentProfile)
    {
    }

    public async Task AddAsync(EmailLog record)
    {
        DB.EmailLogs.Add(record);
        await DB.SaveChangesAsync();
    }

    public IEmailLogQuery ByPublicId(string id)
    {
        currentQuery = from el in currentQuery
                       where el.PublicId == id
                       select el;
        return this;
    }

    public IEmailLogQuery ByStatus(EmailLogStatus status)
    {
        currentQuery = from el in currentQuery
                       where el.Status == status
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

    public EmailLog CreateNew()
    {
        DateTime now = TimeProvider.CurrentTime;

        return new EmailLog
        {
            Id = Guid.NewGuid(),
            PublicId = IdGenerator.New(),
            CreateDate = now,
            UpdateDate = now,
        };
    }

    public async Task RemoveAsync(EmailLog record)
    {
        DB.EmailLogs.Remove(record);
        await DB.SaveChangesAsync();
    }

    public IQueryable<EmailLog> Select()
    {
        return currentQuery;
    }

    public async Task UpdateAsync(EmailLog record)
    {
        DateTime now = TimeProvider.CurrentTime;
        record.UpdateDate = now;
        DB.EmailLogs.Update(record);
        await DB.SaveChangesAsync();
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

    public IEmailLogQuery Skip(int value)
    {
        currentQuery = currentQuery.Skip(value);
        return this;
    }

    public IEmailLogQuery Take(int value)
    {
        currentQuery = currentQuery.Take(value);
        return this;
    }

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
        else
        {
            throw new ArgumentException("Invalid column name");
        }

        return this;
    }
}
