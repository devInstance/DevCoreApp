using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Postgres.Data;
using DevInstance.DevCoreApp.Shared.TestUtils;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DevInstance.DevCoreApp.Tests.Server.Database.Core;

/// <summary>
/// Regression guard for the per-operation unit-of-work pattern (see src/Server/Database/UnitOfWork.md).
/// Fans out many overlapping operations through the real <see cref="QueryRepositoryFactory"/> and
/// asserts none trip EF Core's concurrency detector ("A second operation was started on this
/// context instance..."). If someone regresses the factory into handing back a shared context,
/// these tests start failing.
/// </summary>
public class UnitOfWorkConcurrencyTests
{
    private const int OverlappingOperations = 32;

    private readonly TestOperationContext _operationContext;
    private readonly DbContextOptions _options;
    private readonly IScopeManager _logManager;
    private readonly ITimeProvider _timeProvider;

    // Reuses TestApplicationDbContext / TestOperationContext from OrganizationQueryFilterTests.
    private sealed class TestAppDbContextFactory : IAppDbContextFactory
    {
        private readonly DbContextOptions _options;
        private readonly IOperationContext _operationContext;

        public TestAppDbContextFactory(DbContextOptions options, IOperationContext operationContext)
        {
            _options = options;
            _operationContext = operationContext;
        }

        public ApplicationDbContext CreateDbContext()
            => new TestApplicationDbContext(_options, _operationContext);
    }

    public UnitOfWorkConcurrencyTests()
    {
        _operationContext = new TestOperationContext(); // empty VisibleOrganizationIds → filter bypassed (sees all)
        _logManager = new IScopeManagerMock();
        _timeProvider = TimerProviderMock.CreateTimerProvider();

        // Shared in-memory database, seeded once with a few organizations to read.
        _options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var seed = new TestApplicationDbContext(_options, _operationContext);
        for (int i = 0; i < 3; i++)
        {
            seed.Organizations.Add(new Organization
            {
                Id = Guid.NewGuid(),
                PublicId = $"org-{i}",
                Name = $"Org {i}",
                Code = $"ORG{i}",
                Level = 0,
                Path = $"/ORG{i}",
                Type = "Company",
                CreateDate = DateTime.UtcNow,
                UpdateDate = DateTime.UtcNow
            });
        }
        seed.SaveChanges();
    }

    private QueryRepositoryFactory CreateFactory()
        => new QueryRepositoryFactory(_logManager, _timeProvider, new TestAppDbContextFactory(_options, _operationContext));

    [Fact]
    public async Task ConcurrentReads_ThroughFactory_DoNotCollide()
    {
        var factory = CreateFactory();
        var profile = new UserProfile();

        // Each task opens its own unit of work — separate contexts, so no shared-context collision.
        var tasks = Enumerable.Range(0, OverlappingOperations).Select(_ => Task.Run(async () =>
        {
            await using var repo = factory.Create();
            return await repo.GetOrganizationsQuery(profile).Select().ToListAsync();
        }));

        var results = await Task.WhenAll(tasks); // throws here if any operation tripped the concurrency detector

        Assert.Equal(OverlappingOperations, results.Length);
        Assert.All(results, orgs => Assert.Equal(3, orgs.Count));
    }

    [Fact]
    public async Task ConcurrentWrites_ThroughFactory_DoNotCollide()
    {
        var factory = CreateFactory();
        var profile = new UserProfile();

        var tasks = Enumerable.Range(0, OverlappingOperations).Select(i => Task.Run(async () =>
        {
            await using var repo = factory.Create();
            var query = repo.GetOrganizationsQuery(profile);
            var org = query.CreateNew();
            org.PublicId = $"concurrent-{i}";
            org.Name = $"Concurrent {i}";
            org.Code = $"CC{i}";
            org.Level = 0;
            org.Path = $"/CC{i}";
            org.Type = "Company";
            await query.AddAsync(org);
        }));

        await Task.WhenAll(tasks); // throws if a shared context serialized/collided these writes

        using var verify = new TestApplicationDbContext(_options, _operationContext);
        var written = await verify.Organizations.CountAsync(o => o.PublicId.StartsWith("concurrent-"), TestContext.Current.CancellationToken);
        Assert.Equal(OverlappingOperations, written);
    }

    [Fact]
    public async Task Factory_Create_ReturnsIndependentUnitsOfWork()
    {
        var factory = CreateFactory();
        var profile = new UserProfile();

        // Disposing one repo must not affect another created from the same factory.
        var repoA = factory.Create();
        var repoB = factory.Create();

        var fromB1 = await repoB.GetOrganizationsQuery(profile).Select().ToListAsync(TestContext.Current.CancellationToken);
        await repoA.DisposeAsync(); // disposes A's context only

        var fromB2 = await repoB.GetOrganizationsQuery(profile).Select().ToListAsync(TestContext.Current.CancellationToken);
        await repoB.DisposeAsync();

        Assert.Equal(3, fromB1.Count);
        Assert.Equal(3, fromB2.Count);
    }

    /// <summary>
    /// The scoped IQueryRepository is IAsyncDisposable (via the interface). Migrate/seed code
    /// disposes its DI scope SYNCHRONOUSLY (`using var scope = provider.CreateScope()`), which
    /// throws "type only implements IAsyncDisposable" unless the repository also implements
    /// IDisposable. Guards that regression.
    /// </summary>
    [Fact]
    public void ScopedRepository_ResolvedAndSyncScopeDisposed_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_logManager);
        services.AddSingleton(_timeProvider);
        services.AddScoped<IQueryRepository>(_ =>
            new CoreQueryRepository(_logManager, _timeProvider,
                new TestApplicationDbContext(_options, _operationContext), ownsContext: false));

        using var provider = services.BuildServiceProvider();

        var ex = Record.Exception(() =>
        {
            // Synchronous scope teardown — the exact pattern used by MigrateAndSeedAsync's scope.
            using var scope = provider.CreateScope();
            _ = scope.ServiceProvider.GetRequiredService<IQueryRepository>();
        });

        Assert.Null(ex);
    }
}
