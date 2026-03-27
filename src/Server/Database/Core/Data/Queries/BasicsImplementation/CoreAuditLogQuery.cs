using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public class CoreAuditLogQuery : CoreBaseQuery<AuditLog, CoreAuditLogQuery>, IAuditLogQuery
{
    private CoreAuditLogQuery(IQueryable<AuditLog> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreAuditLogQuery(IScopeManager logManager,
                             ITimeProvider timeProvider,
                             ApplicationDbContext dB,
                             UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public IAuditLogQuery ByPublicId(string id) => ByGuidIdHelper(id);

    public IAuditLogQuery ByTableName(string tableName)
    {
        currentQuery = from al in currentQuery
                       where al.TableName == tableName
                       select al;
        return this;
    }

    public IAuditLogQuery ByRecordId(string recordId)
    {
        currentQuery = from al in currentQuery
                       where al.RecordId == recordId
                       select al;
        return this;
    }

    public IAuditLogQuery ByAction(AuditAction action)
    {
        currentQuery = from al in currentQuery
                       where al.Action == action
                       select al;
        return this;
    }

    public IAuditLogQuery BySource(AuditSource source)
    {
        currentQuery = from al in currentQuery
                       where al.Source == source
                       select al;
        return this;
    }

    public IAuditLogQuery ByChangedByUserId(Guid userId)
    {
        currentQuery = from al in currentQuery
                       where al.ChangedByUserId == userId
                       select al;
        return this;
    }

    public IAuditLogQuery ByDateRange(DateTime? start, DateTime? end)
    {
        if (start.HasValue)
        {
            currentQuery = from al in currentQuery
                           where al.ChangedAt >= start.Value
                           select al;
        }
        if (end.HasValue)
        {
            currentQuery = from al in currentQuery
                           where al.ChangedAt <= end.Value
                           select al;
        }
        return this;
    }

    public AuditLog CreateNew()
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            ChangedAt = TimeProvider.CurrentTime,
        };
    }

    public IAuditLogQuery Clone()
    {
        return new CoreAuditLogQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public IAuditLogQuery Search(string search)
    {
        currentQuery = from al in currentQuery
                       where al.TableName.Contains(search) ||
                             al.RecordId.Contains(search) ||
                             (al.IpAddress != null && al.IpAddress.Contains(search)) ||
                             (al.CorrelationId != null && al.CorrelationId.Contains(search))
                       select al;
        return this;
    }

    public IAuditLogQuery Skip(int value) => SkipHelper(value);

    public IAuditLogQuery Take(int value) => TakeHelper(value);

    public IAuditLogQuery SortBy(string column, bool isAsc)
    {
        this.IsAsc = isAsc;

        if (string.Compare(column, "changedat", true) == 0)
        {
            currentQuery = isAsc
                ? (from al in currentQuery orderby al.ChangedAt select al)
                : (from al in currentQuery orderby al.ChangedAt descending select al);
            SortedBy = "changedat";
        }
        else if (string.Compare(column, "tablename", true) == 0)
        {
            currentQuery = isAsc
                ? (from al in currentQuery orderby al.TableName select al)
                : (from al in currentQuery orderby al.TableName descending select al);
            SortedBy = "tablename";
        }
        else if (string.Compare(column, "recordid", true) == 0)
        {
            currentQuery = isAsc
                ? (from al in currentQuery orderby al.RecordId select al)
                : (from al in currentQuery orderby al.RecordId descending select al);
            SortedBy = "recordid";
        }
        else if (string.Compare(column, "action", true) == 0)
        {
            currentQuery = isAsc
                ? (from al in currentQuery orderby al.Action select al)
                : (from al in currentQuery orderby al.Action descending select al);
            SortedBy = "action";
        }
        else if (string.Compare(column, "source", true) == 0)
        {
            currentQuery = isAsc
                ? (from al in currentQuery orderby al.Source select al)
                : (from al in currentQuery orderby al.Source descending select al);
            SortedBy = "source";
        }
        else
        {
            throw new ArgumentException("Invalid column name");
        }

        return this;
    }
}
