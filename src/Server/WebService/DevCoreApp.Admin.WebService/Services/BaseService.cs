using DevInstance.LogScope;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.DevCoreApp.Server.Admin.WebService.Authentication;
using DevInstance.WebServiceToolkit.Database.Queries;
using DevInstance.WebServiceToolkit.Common.Model;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Services;

public abstract class BaseService
{
    private IScopeLog log;

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

    protected static ModelList<T> CreateList<T>()
    {
        return new ModelList<T>()
        {
            TotalCount = 0,
            Count = 0,
            PagesCount = 0,
            Page = 0,
            Items = new T[0]
        };
    }

    protected static ModelList<T> ApplyItems<T>(ModelList<T> list, int totalItemsCount, T[] items, int? top, int? page)
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

        list.TotalCount = totalItemsCount;
        list.Count = items.Length;
        list.PagesCount = totalPageCount;
        list.Page = pageIndex;
        list.Items = items;

        return list;
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

    protected static T ApplyFilters<T, M>(ModelList<M> list, T q, int? filter, string? search) where T : IQSearchable<T>
    {
        if (filter != null)
        {
            // TODO: add some filters
            //if ((filter & ItemFilter.All) != 0)
            //    coreQuery = coreQuery.DoSomething();
        }

        if (!String.IsNullOrEmpty(search))
        {
            q = q.Search(search);
            list.Search = search;
        }

        return q;
    }

    protected static T ApplySorting<T, M>(ModelList<M> list, T q, string? sortBy, bool? isAsc) where T : IQSortable<T>
    {
        if (!string.IsNullOrEmpty(sortBy))
        {
            try
            {
                q = q.SortBy(sortBy, isAsc ?? true);
                list.SortBy = sortBy;
                list.IsAsc = isAsc ?? true;
            }
            catch (ArgumentException)
            {
                // ignore
            }
        }

        return q;
    }

}