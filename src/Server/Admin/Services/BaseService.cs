using System;
using DevInstance.LogScope;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.WebServiceToolkit.Database.Queries;
using DevInstance.WebServiceToolkit.Common.Model;

namespace DevInstance.DevCoreApp.Server.Admin.Services;

public abstract class BaseService
{
    private IScopeLog log;

    public ITimeProvider TimeProvider { get; }

    // Per-operation unit of work. Blazor-facing service methods open one via
    // `await using var repo = RepositoryFactory.Create();` so concurrent components on a
    // circuit never share a context. The old shared scoped `Repository` is intentionally
    // gone — do not reintroduce a scoped IQueryRepository here. See src/Server/Database/UnitOfWork.md.
    public IQueryRepositoryFactory RepositoryFactory { get; }

    public IAuthorizationContext AuthorizationContext { get; }

    protected TimeZoneInfo? UserTimeZone => ResolveTimeZone(AuthorizationContext.CurrentProfile?.TimeZoneId);

    /// <summary>
    /// Resolves a TimeZoneInfo from a stored TimeZoneId, returning null for an unset or
    /// unrecognized id rather than throwing. Static so callers outside a service instance
    /// (background handlers, mappers) can reuse the same lenient behavior.
    /// </summary>
    protected static TimeZoneInfo? ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrEmpty(timeZoneId)) return null;
        try { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch { return null; }
    }

    public BaseService(IScopeManager logManager,
                        ITimeProvider timeProvider,
                        IQueryRepositoryFactory repositoryFactory,
                        IAuthorizationContext authorizationContext)
    {
        log = logManager.CreateLogger(this);

        TimeProvider = timeProvider;
        RepositoryFactory = repositoryFactory;
        AuthorizationContext = authorizationContext;
    }
}