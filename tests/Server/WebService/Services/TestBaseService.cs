using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using DevInstance.DevCoreApp.Server.Services;
using DevInstance.DevCoreApp.Server.Admin.WebService.Authentication;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Tests.Services;

internal class TestBaseServiceEntity : DatabaseEntityObject
{

}
public interface ITestBaseServiceQuery : /*IModelQuery<TestBaseServiceEntity, ITestBaseServiceQuery>,*/
                                   IQSearchable<ITestBaseServiceQuery>,
                                    IQPageable<ITestBaseServiceQuery>
{

}

internal class TestBaseService : BaseService
{
    public TestBaseService(IScopeManager logManager, ITimeProvider timeProvider, IQueryRepository query, IAuthorizationContext authorizationContext) 
        : base(logManager, timeProvider, query, authorizationContext)
    {
    }

    public static ModelList<TestBaseServiceEntity> CreateListPageForTest(int totalItemsCount, TestBaseServiceEntity[] items, int? top, int? page)
    {
        return CreateListPage<TestBaseServiceEntity>(totalItemsCount, items, top, page);
    }

    public static ITestBaseServiceQuery ApplyPagesForTest(ITestBaseServiceQuery q, int? top, int? page)
    {
        return ApplyPages(q, top, page);
    }

    public static ITestBaseServiceQuery ApplyFiltersForTest(ITestBaseServiceQuery coreQuery, int? filter, string search)
    {
        return ApplyFilters(coreQuery, filter, search);
    }
}
