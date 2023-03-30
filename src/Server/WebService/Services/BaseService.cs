using DevInstance.LogScope;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.WebService.Indentity;
using DevInstance.DevCoreApp.Shared.Model;
using System;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Shared.Utils;

namespace DevInstance.DevCoreApp.Server.Services;

public abstract class BaseService
{
    protected readonly IScopeLog log;

    public ITimeProvider TimeProvider { get; }
    public IQueryRepository Repository { get; }

    public IAuthorizationContext AuthorizationContext { get; }

    public BaseService(IScopeManager logManager,
                        ITimeProvider timeProvider,
                        IQueryRepository query,
                        IAuthorizationContext authorizationContext)
    {
        log = logManager.CreateLogger(this);

        TimeProvider = timeProvider;
        Repository = query;
        AuthorizationContext = authorizationContext;
    }

    protected static ModelList<T> CreateListPage<T>(int totalItemsCount, T[] items, int? top, int? page)
    {
        var pageIndex = 0;
        var totalPageCount = 1;
        if (top.HasValue && top.Value > 0)
        {
            totalPageCount = (int)Math.Ceiling((double)totalItemsCount / (double)top.Value);
        }
        if (page.HasValue && page.Value >= 0)
        {
            pageIndex = page.Value;
            if (pageIndex >= totalPageCount)
            {
                pageIndex = totalPageCount - 1;
            }
        }
        return new ModelList<T>()
        {
            TotalCount = totalItemsCount,
            Count = items.Length,
            PagesCount = totalPageCount,
            Page = pageIndex,
            Items = items
        };
    }

    protected static T ApplyPages<T>(T q, int? top, int? page) where T : IQPageable<T>
    {
        if (top.HasValue && top.Value > 0)
        {
            if (page.HasValue && page.Value > 0)
            {
                q = q.Skip(page.Value * top.Value);
            }
            q = q.Take(top.Value);
        }

        return q;
    }

    protected static T ApplyFilters<T>(T coreQuery, int? filter, string search) where T : IQSearchable<T>
    {
        if (filter != null)
        {
            // TODO: add some filters
            //if ((filter & ItemFilter.All) != 0)
            //    coreQuery = coreQuery.DoSomething();
        }

        if (!String.IsNullOrEmpty(search))
        {
            coreQuery = coreQuery.Search(search);
        }

        return coreQuery;
    }

}