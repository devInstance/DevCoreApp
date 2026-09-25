using System;
using DevInstance.LogScope;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Shared.Utils.Core;
using DevInstance.DevCoreApp.Server.Admin.Services.Core.Authentication;
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
    /// unrecognized id rather than throwing. Kept as a protected shorthand for services; the
    /// behavior itself lives in <see cref="DateTimeExtensions.ResolveTimeZone"/>, which
    /// <c>ICurrentUserContext</c> shares so pages and services cannot drift apart.
    /// </summary>
    protected static TimeZoneInfo? ResolveTimeZone(string? timeZoneId)
        => DateTimeExtensions.ResolveTimeZone(timeZoneId);

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