using DevInstance.DevCoreApp.Server.Database.Core.Models.BackgroundTasks;
using DevInstance.DevCoreApp.Shared.Model.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Tests.Server.Database.Core;

/// <summary>
/// Covers the write side of organization scoping. The read-side filter is deliberately
/// fail-open (an empty VisibleOrganizationIds disables it), so an unstamped row reads back fine
/// for an unscoped user and then vanishes for every real one. These tests pin the stamping
/// behavior that keeps that from happening silently.
///
/// Reuses TestApplicationDbContext / TestScopedEntity / TestOperationContext from
/// OrganizationQueryFilterTests.
/// </summary>
public class OrganizationStampInterceptorTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid OtherOrgId = Guid.NewGuid();

    private readonly TestOperationContext _operationContext = new();
    private readonly DbContextOptions _options = new DbContextOptionsBuilder()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

    private TestApplicationDbContext CreateContext() => new(_options, _operationContext);

    [Fact]
    public async Task Insert_WithoutOrganization_IsStampedFromOperationContext()
    {
        _operationContext.PrimaryOrganizationId = OrgId;
        _operationContext.VisibleOrganizationIds = new HashSet<Guid> { OrgId };

        using (var ctx = CreateContext())
        {
            ctx.TestScopedEntities.Add(new TestScopedEntity
            {
                Id = Guid.NewGuid(),
                PublicId = "s1",
                Title = "Unstamped on create",
                CreateDate = DateTime.UtcNow,
                UpdateDate = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
        }

        using var readCtx = CreateContext();
        var saved = await readCtx.TestScopedEntities.SingleAsync();
        Assert.Equal(OrgId, saved.OrganizationId);
    }

    [Fact]
    public async Task Insert_WithExplicitOrganization_IsLeftAlone()
    {
        // A caller may deliberately write into another organization — e.g. a background job
        // stamping rows from the parent record's organization rather than its own context.
        _operationContext.PrimaryOrganizationId = OrgId;
        _operationContext.VisibleOrganizationIds = new HashSet<Guid> { OrgId, OtherOrgId };

        using (var ctx = CreateContext())
        {
            ctx.TestScopedEntities.Add(new TestScopedEntity
            {
                Id = Guid.NewGuid(),
                PublicId = "s2",
                Title = "Explicitly scoped elsewhere",
                OrganizationId = OtherOrgId,
                CreateDate = DateTime.UtcNow,
                UpdateDate = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
        }

        using var readCtx = CreateContext();
        var saved = await readCtx.TestScopedEntities.SingleAsync();
        Assert.Equal(OtherOrgId, saved.OrganizationId);
    }

    [Fact]
    public async Task Insert_WithNoResolvableOrganization_Throws()
    {
        _operationContext.PrimaryOrganizationId = null;
        _operationContext.VisibleOrganizationIds = new HashSet<Guid>();

        using var ctx = CreateContext();
        ctx.TestScopedEntities.Add(new TestScopedEntity
        {
            Id = Guid.NewGuid(),
            PublicId = "s3",
            Title = "Nothing to scope it to",
            CreateDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => ctx.SaveChangesAsync());
        Assert.Contains(nameof(TestScopedEntity), ex.Message);
    }

    [Fact]
    public async Task BackgroundTask_IsAllowedThroughUnscoped()
    {
        // BackgroundTask rows are built in a reset scope with no ambient organization. Guarding
        // them would fail unauthenticated confirmation/reset mail outright rather than merely hide
        // a job row, so they are exempt. BackgroundWorker assigns the organization when the
        // submitter supplied one.
        _operationContext.PrimaryOrganizationId = null;
        _operationContext.VisibleOrganizationIds = new HashSet<Guid>();

        using var ctx = CreateContext();
        ctx.BackgroundTasks.Add(new BackgroundTask
        {
            Id = Guid.NewGuid(),
            PublicId = "bt1",
            TaskType = "SendEmail",
            Payload = "{}",
            Status = BackgroundTaskStatus.Queued,
            ScheduledAt = DateTime.UtcNow,
            CreateDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow
        });

        await ctx.SaveChangesAsync();

        var saved = await ctx.BackgroundTasks.SingleAsync();
        Assert.Equal(Guid.Empty, saved.OrganizationId);
    }
}
