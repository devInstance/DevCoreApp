using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using DevInstance.DevCoreApp.Shared.Model.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Tests.Server.Database.Core;

/// <summary>
/// A sample entity implementing IOrganizationScoped for testing the global query filter.
/// </summary>
public class TestScopedEntity : DatabaseObject, IOrganizationScoped
{
    public string Title { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
}

/// <summary>
/// Test DbContext that adds a TestScopedEntity DbSet and inherits the global query filters
/// from ApplicationDbContext.
/// </summary>
public class TestApplicationDbContext : ApplicationDbContext
{
    public DbSet<TestScopedEntity> TestScopedEntities { get; set; }

    public TestApplicationDbContext(DbContextOptions options, IOperationContext operationContext)
        : base(options, operationContext)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<TestScopedEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
    }
}

/// <summary>
/// Mutable IOperationContext for tests.
/// </summary>
public class TestOperationContext : IOperationContext
{
    public Guid? UserId { get; set; }
    public Guid? PrimaryOrganizationId { get; set; }
    public IReadOnlySet<Guid> VisibleOrganizationIds { get; set; } = new HashSet<Guid>();
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
}

public class OrganizationQueryFilterTests
{
    private static readonly Guid RootOrgId = Guid.NewGuid();
    private static readonly Guid EastRegionId = Guid.NewGuid();
    private static readonly Guid WestRegionId = Guid.NewGuid();
    private static readonly Guid NewYorkId = Guid.NewGuid();
    private static readonly Guid BostonId = Guid.NewGuid();
    private static readonly Guid LosAngelesId = Guid.NewGuid();

    private readonly TestOperationContext _operationContext;
    private readonly DbContextOptions _options;

    public OrganizationQueryFilterTests()
    {
        _operationContext = new TestOperationContext();
        _options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Seed with an empty context (no filter — VisibleOrganizationIds is empty which bypasses filter)
        using var seedCtx = CreateContext();
        SeedOrganizations(seedCtx);
        SeedTestEntities(seedCtx);
        seedCtx.SaveChanges();
    }

    private TestApplicationDbContext CreateContext()
    {
        return new TestApplicationDbContext(_options, _operationContext);
    }

    private static void SeedOrganizations(TestApplicationDbContext ctx)
    {
        ctx.Organizations.AddRange(
            new Organization { Id = RootOrgId, PublicId = "root", Name = "Acme Corp", Code = "ACME", Level = 0, Path = "/ACME", Type = "Company", CreateDate = DateTime.UtcNow, UpdateDate = DateTime.UtcNow },
            new Organization { Id = EastRegionId, PublicId = "east", Name = "East Region", Code = "EAST", ParentId = RootOrgId, Level = 1, Path = "/ACME/EAST", Type = "Region", CreateDate = DateTime.UtcNow, UpdateDate = DateTime.UtcNow },
            new Organization { Id = WestRegionId, PublicId = "west", Name = "West Region", Code = "WEST", ParentId = RootOrgId, Level = 1, Path = "/ACME/WEST", Type = "Region", CreateDate = DateTime.UtcNow, UpdateDate = DateTime.UtcNow },
            new Organization { Id = NewYorkId, PublicId = "nyc", Name = "New York Office", Code = "NYC", ParentId = EastRegionId, Level = 2, Path = "/ACME/EAST/NYC", Type = "Office", CreateDate = DateTime.UtcNow, UpdateDate = DateTime.UtcNow },
            new Organization { Id = BostonId, PublicId = "bos", Name = "Boston Office", Code = "BOS", ParentId = EastRegionId, Level = 2, Path = "/ACME/EAST/BOS", Type = "Office", CreateDate = DateTime.UtcNow, UpdateDate = DateTime.UtcNow },
            new Organization { Id = LosAngelesId, PublicId = "la", Name = "Los Angeles Office", Code = "LA", ParentId = WestRegionId, Level = 2, Path = "/ACME/WEST/LA", Type = "Office", CreateDate = DateTime.UtcNow, UpdateDate = DateTime.UtcNow }
        );
    }

    private static void SeedTestEntities(TestApplicationDbContext ctx)
    {
        ctx.TestScopedEntities.AddRange(
            new TestScopedEntity { Id = Guid.NewGuid(), PublicId = "e1", Title = "East HQ Report", OrganizationId = EastRegionId, CreateDate = DateTime.UtcNow, UpdateDate = DateTime.UtcNow },
            new TestScopedEntity { Id = Guid.NewGuid(), PublicId = "e2", Title = "NYC Invoice", OrganizationId = NewYorkId, CreateDate = DateTime.UtcNow, UpdateDate = DateTime.UtcNow },
            new TestScopedEntity { Id = Guid.NewGuid(), PublicId = "e3", Title = "Boston Invoice", OrganizationId = BostonId, CreateDate = DateTime.UtcNow, UpdateDate = DateTime.UtcNow },
            new TestScopedEntity { Id = Guid.NewGuid(), PublicId = "e4", Title = "West HQ Report", OrganizationId = WestRegionId, CreateDate = DateTime.UtcNow, UpdateDate = DateTime.UtcNow },
            new TestScopedEntity { Id = Guid.NewGuid(), PublicId = "e5", Title = "LA Invoice", OrganizationId = LosAngelesId, CreateDate = DateTime.UtcNow, UpdateDate = DateTime.UtcNow }
        );
    }

    [Fact]
    public async Task WithChildrenScope_EastRegion_SeesEastNewYorkBoston_NotWest()
    {
        // Arrange: User with WithChildren scope on East Region
        _operationContext.VisibleOrganizationIds = new HashSet<Guid> { EastRegionId, NewYorkId, BostonId };
        _operationContext.PrimaryOrganizationId = EastRegionId;

        // Act
        using var ctx = CreateContext();
        var results = await ctx.TestScopedEntities.ToListAsync();

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Contains(results, e => e.Title == "East HQ Report");
        Assert.Contains(results, e => e.Title == "NYC Invoice");
        Assert.Contains(results, e => e.Title == "Boston Invoice");
        Assert.DoesNotContain(results, e => e.Title == "West HQ Report");
        Assert.DoesNotContain(results, e => e.Title == "LA Invoice");
    }

    [Fact]
    public async Task SelfScope_NewYorkOnly_SeesOnlyNewYork()
    {
        // Arrange: User with Self scope on New York only
        _operationContext.VisibleOrganizationIds = new HashSet<Guid> { NewYorkId };
        _operationContext.PrimaryOrganizationId = NewYorkId;

        // Act
        using var ctx = CreateContext();
        var results = await ctx.TestScopedEntities.ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal("NYC Invoice", results[0].Title);
    }

    [Fact]
    public async Task RootScope_SeesAllEntities()
    {
        // Arrange: Top admin with WithChildren scope on root — sees everything
        _operationContext.VisibleOrganizationIds = new HashSet<Guid>
        {
            RootOrgId, EastRegionId, WestRegionId, NewYorkId, BostonId, LosAngelesId
        };

        // Act
        using var ctx = CreateContext();
        var results = await ctx.TestScopedEntities.ToListAsync();

        // Assert
        Assert.Equal(5, results.Count);
    }

    [Fact]
    public async Task EmptyVisibleOrgs_SystemOperation_SeesAllEntities()
    {
        // Arrange: System operation — empty VisibleOrganizationIds bypasses filter
        _operationContext.VisibleOrganizationIds = new HashSet<Guid>();

        // Act
        using var ctx = CreateContext();
        var results = await ctx.TestScopedEntities.ToListAsync();

        // Assert: filter is disabled, all 5 entities visible
        Assert.Equal(5, results.Count);
    }

    [Fact]
    public async Task NullVisibleOrgs_SystemOperation_SeesAllEntities()
    {
        // Arrange: System operation — null VisibleOrganizationIds bypasses filter
        _operationContext.VisibleOrganizationIds = null;

        // Act
        using var ctx = CreateContext();
        var results = await ctx.TestScopedEntities.ToListAsync();

        // Assert: filter is disabled, all 5 entities visible
        Assert.Equal(5, results.Count);
    }

    [Fact]
    public async Task NonScopedEntities_NotAffectedByFilter()
    {
        // Arrange: Even with a restricted scope, Organization (not IOrganizationScoped) is unfiltered
        _operationContext.VisibleOrganizationIds = new HashSet<Guid> { NewYorkId };

        // Act
        using var ctx = CreateContext();
        var orgs = await ctx.Organizations.ToListAsync();

        // Assert: All 6 organizations visible — Organization is not IOrganizationScoped
        Assert.Equal(6, orgs.Count);
    }
}
