using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public class CoreUserLoginHistoryQuery : CoreBaseQuery<UserLoginHistory, CoreUserLoginHistoryQuery>, IUserLoginHistoryQuery
{
    private CoreUserLoginHistoryQuery(IQueryable<UserLoginHistory> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreUserLoginHistoryQuery(IScopeManager logManager,
                             ITimeProvider timeProvider,
                             ApplicationDbContext dB,
                             UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public IUserLoginHistoryQuery ByPublicId(string id) => ByGuidIdHelper(id);

    public IUserLoginHistoryQuery ByUserId(Guid userId)
    {
        currentQuery = from ulh in currentQuery
                       where ulh.UserId == userId
                       select ulh;
        return this;
    }

    public IUserLoginHistoryQuery BySuccess(bool success)
    {
        currentQuery = from ulh in currentQuery
                       where ulh.Success == success
                       select ulh;
        return this;
    }

    public IUserLoginHistoryQuery ByDateRange(DateTime? start, DateTime? end)
    {
        if (start.HasValue)
        {
            currentQuery = from ulh in currentQuery
                           where ulh.LoginAt >= start.Value
                           select ulh;
        }
        if (end.HasValue)
        {
            currentQuery = from ulh in currentQuery
                           where ulh.LoginAt <= end.Value
                           select ulh;
        }
        return this;
    }

    public IUserLoginHistoryQuery Clone()
    {
        return new CoreUserLoginHistoryQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public UserLoginHistory CreateNew()
    {
        return new UserLoginHistory
        {
            Id = Guid.NewGuid(),
            LoginAt = TimeProvider.CurrentTime,
        };
    }

    public IUserLoginHistoryQuery Search(string search)
    {
        currentQuery = from ulh in currentQuery
                       where (ulh.IpAddress != null && ulh.IpAddress.Contains(search)) ||
                             (ulh.FailureReason != null && ulh.FailureReason.Contains(search))
                       select ulh;
        return this;
    }

    public IUserLoginHistoryQuery Skip(int value) => SkipHelper(value);

    public IUserLoginHistoryQuery Take(int value) => TakeHelper(value);

    public IUserLoginHistoryQuery SortBy(string column, bool isAsc)
    {
        this.IsAsc = isAsc;

        if (string.Compare(column, "loginat", true) == 0)
        {
            currentQuery = isAsc
                ? (from ulh in currentQuery orderby ulh.LoginAt select ulh)
                : (from ulh in currentQuery orderby ulh.LoginAt descending select ulh);
            SortedBy = "loginat";
        }
        else if (string.Compare(column, "ipaddress", true) == 0)
        {
            currentQuery = isAsc
                ? (from ulh in currentQuery orderby ulh.IpAddress select ulh)
                : (from ulh in currentQuery orderby ulh.IpAddress descending select ulh);
            SortedBy = "ipaddress";
        }
        else if (string.Compare(column, "success", true) == 0)
        {
            currentQuery = isAsc
                ? (from ulh in currentQuery orderby ulh.Success select ulh)
                : (from ulh in currentQuery orderby ulh.Success descending select ulh);
            SortedBy = "success";
        }
        else
        {
            throw new ArgumentException("Invalid column name");
        }

        return this;
    }
}
