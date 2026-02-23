using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.WebServiceToolkit.Database.Queries;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IUserLoginHistoryQuery : IModelQuery<UserLoginHistory, IUserLoginHistoryQuery>,
        IQSearchable<IUserLoginHistoryQuery>,
        IQPageable<IUserLoginHistoryQuery>,
        IQSortable<IUserLoginHistoryQuery>
{
    IQueryable<UserLoginHistory> Select();

    IUserLoginHistoryQuery ByUserId(Guid userId);
    IUserLoginHistoryQuery BySuccess(bool success);
    IUserLoginHistoryQuery ByDateRange(DateTime? start, DateTime? end);
}
