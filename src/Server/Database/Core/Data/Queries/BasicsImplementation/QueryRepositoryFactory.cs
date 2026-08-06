using DevInstance.LogScope;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Shared.Utils.Core;

namespace DevInstance.DevCoreApp.Server.Database.Postgres.Data;

/// <summary>
/// Creates a per-operation <see cref="IQueryRepository"/> over a fresh context pulled from
/// <see cref="IAppDbContextFactory"/>. The repository owns that context and disposes it when
/// the caller's <c>await using</c> block ends.
/// </summary>
public sealed class QueryRepositoryFactory : IQueryRepositoryFactory
{
    private readonly IScopeManager _logManager;
    private readonly ITimeProvider _timeProvider;
    private readonly IAppDbContextFactory _contextFactory;

    public QueryRepositoryFactory(IScopeManager logManager,
                                  ITimeProvider timeProvider,
                                  IAppDbContextFactory contextFactory)
    {
        _logManager = logManager;
        _timeProvider = timeProvider;
        _contextFactory = contextFactory;
    }

    public IQueryRepository Create()
        => new CoreQueryRepository(_logManager, _timeProvider, _contextFactory.CreateDbContext(), ownsContext: true);
}
