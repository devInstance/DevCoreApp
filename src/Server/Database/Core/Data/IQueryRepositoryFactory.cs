namespace DevInstance.DevCoreApp.Server.Database.Core.Data;

/// <summary>
/// Hands back an <see cref="IQueryRepository"/> that owns a fresh short-lived context — one
/// unit of work per logical operation. Blazor-facing services open one per method via
/// <c>await using var repo = RepositoryFactory.Create();</c> so concurrent components on a
/// circuit never share a context. See <c>src/Server/Database/UnitOfWork.md</c>.
/// </summary>
public interface IQueryRepositoryFactory
{
    IQueryRepository Create();
}
