using DevInstance.DevCoreApp.Server.Admin.Services;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;
using DevInstance.WebServiceToolkit.Database.Queries;

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
        return ModelListResult.CreateList(items, totalItemsCount, top, page);
    }

    public static ITestBaseServiceQuery ApplyPagesForTest(ITestBaseServiceQuery q, int? top, int? page)
    {
        //TODO:
        //return ApplyPages(q, top, page);
        return q;
    }

    public static ITestBaseServiceQuery ApplyFiltersForTest(ITestBaseServiceQuery coreQuery, int? filter, string search)
    {
        //TODO:
        //return ApplyFilters(coreQuery, filter, search);
        return coreQuery;
    }
}
