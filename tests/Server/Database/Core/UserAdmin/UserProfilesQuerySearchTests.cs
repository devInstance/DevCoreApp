using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Postgres.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.TestUtils.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace DevInstance.DevCoreApp.Tests.Server.Database.Core.UserAdmin;

/// <summary>
/// <c>Search()</c> is case-insensitive in every shared query (both sides lower-cased), so the search
/// box agrees with <c>SearchHighlighter</c>, which matches with <c>OrdinalIgnoreCase</c>.
/// Runs the real query over InMemory, the same way the API would.
/// </summary>
public class UserProfilesQuerySearchTests
{
    private readonly DbContextOptions _options;
    private readonly TestOperationContext _operationContext = new();

    public UserProfilesQuerySearchTests()
    {
        _options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var seed = new TestApplicationDbContext(_options, _operationContext);
        seed.UserProfiles.Add(Profile("p-plain", "Plain", "plain.user@test"));
        seed.UserProfiles.Add(Profile("p-other", "Other", "other.user@test"));
        seed.SaveChanges();
    }

    private static UserProfile Profile(string publicId, string firstName, string email) => new()
    {
        Id = Guid.NewGuid(),
        PublicId = publicId,
        Email = email,
        FirstName = firstName,
        MiddleName = "",
        LastName = "Last",
        PhoneNumber = "",
        ApplicationUserId = Guid.NewGuid(),
        Status = UserStatus.LIVE,
        CreateDate = DateTime.UtcNow,
        UpdateDate = DateTime.UtcNow
    };

    private QueryRepositoryFactory CreateFactory()
        => new(new IScopeManagerMock(), TimerProviderMock.CreateTimerProvider(), new TestAppDbContextFactory(_options, _operationContext));

    [Theory]
    [InlineData("PLain")]
    [InlineData("plain")]
    [InlineData("PLAIN.USER")]
    public async Task Search_IgnoresCase(string term)
    {
        await using var repo = CreateFactory().Create();

        var found = await repo.GetUserProfilesQuery(new UserProfile()).Search(term).Select()
            .ToListAsync(TestContext.Current.CancellationToken);

        var profile = Assert.Single(found);
        Assert.Equal("p-plain", profile.PublicId);
    }

    private sealed class TestAppDbContextFactory(DbContextOptions options, IOperationContext operationContext) : IAppDbContextFactory
    {
        public ApplicationDbContext CreateDbContext() => new TestApplicationDbContext(options, operationContext);
    }
}
