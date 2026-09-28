using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Postgres.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.TestUtils.Core;
using DevInstance.WebServiceToolkit.Database.Queries.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DevInstance.DevCoreApp.Tests.Server.Database.Core.UserAdmin;

/// <summary>
/// <c>UserProfiles.ApplicationUserId</c> has no foreign key, so profiles can outlive their Identity
/// user. The users list counted and paged those orphans, then dropped them: a wrong total and
/// short or empty pages. <see cref="IUserProfilesQuery.WithApplicationUser"/> filters them in SQL.
/// </summary>
public class UserProfilesQueryTests
{
    private const int WithUser = 4;
    private const int Orphaned = 3;

    private readonly DbContextOptions _options;
    private readonly TestOperationContext _operationContext = new();

    public UserProfilesQueryTests()
    {
        _options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var seed = new TestApplicationDbContext(_options, _operationContext);
        for (int i = 0; i < WithUser + Orphaned; i++)
        {
            var userId = Guid.NewGuid();
            if (i < WithUser)
            {
                seed.Users.Add(new ApplicationUser { Id = userId, UserName = $"user{i}@test", Email = $"user{i}@test" });
            }

            // Orphans sort first, the way they showed up in the real list.
            var email = i < WithUser ? $"b-user{i}@test" : $"a-orphan{i}@test";
            seed.UserProfiles.Add(new UserProfile
            {
                Id = Guid.NewGuid(),
                PublicId = $"profile-{i}",
                Email = email,
                FirstName = "First",
                MiddleName = "",
                LastName = "Last",
                PhoneNumber = "",
                ApplicationUserId = userId,
                Status = UserStatus.LIVE,
                CreateDate = DateTime.UtcNow,
                UpdateDate = DateTime.UtcNow
            });
        }
        seed.SaveChanges();
    }

    private IUserProfilesQuery Query(IQueryRepository repo)
        => repo.GetUserProfilesQuery(new UserProfile()).WithApplicationUser().SortBy("Email", true);

    private QueryRepositoryFactory CreateFactory()
        => new(new IScopeManagerMock(), TimerProviderMock.CreateTimerProvider(), new TestAppDbContextFactory(_options, _operationContext));

    [Fact]
    public async Task WithApplicationUser_CountsOnlyProfilesThatHaveAnAccount()
    {
        await using var repo = CreateFactory().Create();

        var count = await Query(repo).Select().CountAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WithUser, count);
    }

    [Fact]
    public async Task WithApplicationUser_PagesAreFullAndAddUpToTheCount()
    {
        await using var repo = CreateFactory().Create();

        var first = await Query(repo).Paginate(3, 0).Select().ToListAsync(TestContext.Current.CancellationToken);
        var second = await Query(repo).Paginate(3, 1).Select().ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, first.Count);
        Assert.Single(second);
        Assert.All(first.Concat(second), p => Assert.StartsWith("b-user", p.Email));
    }

    private sealed class TestAppDbContextFactory(DbContextOptions options, IOperationContext operationContext) : IAppDbContextFactory
    {
        public ApplicationDbContext CreateDbContext() => new TestApplicationDbContext(options, operationContext);
    }
}
