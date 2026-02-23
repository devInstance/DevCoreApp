using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core;

public abstract class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly IOperationContext _operationContext;

    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<GridProfile> GridProfiles { get; set; }
    public DbSet<EmailLog> EmailLogs { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<UserOrganization> UserOrganizations { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    public ApplicationDbContext(DbContextOptions options, IOperationContext operationContext)
            : base(options)
    {
        _operationContext = operationContext;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.AddInterceptors(new AuditInterceptor(_operationContext));
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<GridProfile>()
            .HasIndex(g => new { g.UserProfileId, g.GridName, g.ProfileName })
            .IsUnique();

        builder.Entity<Organization>(entity =>
        {
            entity.HasOne(o => o.Parent)
                .WithMany(o => o.Children)
                .HasForeignKey(o => o.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(o => o.Path);
            entity.HasIndex(o => o.Code);
            entity.HasIndex(o => o.ParentId);
        });

        builder.Entity<Tenant>(entity =>
        {
            entity.HasOne(t => t.RootOrganization)
                .WithOne()
                .HasForeignKey<Tenant>(t => t.RootOrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(t => t.Subdomain)
                .IsUnique();
        });

        builder.Entity<UserOrganization>(entity =>
        {
            entity.HasOne(uo => uo.User)
                .WithMany(u => u.UserOrganizations)
                .HasForeignKey(uo => uo.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(uo => uo.Organization)
                .WithMany()
                .HasForeignKey(uo => uo.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(uo => new { uo.UserId, uo.OrganizationId })
                .IsUnique();
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne(u => u.PrimaryOrganization)
                .WithMany()
                .HasForeignKey(u => u.PrimaryOrganizationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(a => new { a.TableName, a.RecordId, a.ChangedAt });
        });

        ApplyOrganizationQueryFilters(builder);
    }

    private void ApplyOrganizationQueryFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(IOrganizationScoped).IsAssignableFrom(entityType.ClrType))
                continue;

            var method = typeof(ApplicationDbContext)
                .GetMethod(nameof(ApplyFilterToEntity),
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(this, new object[] { builder });
        }
    }

    private void ApplyFilterToEntity<T>(ModelBuilder builder) where T : class, IOrganizationScoped
    {
        builder.Entity<T>().HasQueryFilter(e =>
            _operationContext.VisibleOrganizationIds == null ||
            _operationContext.VisibleOrganizationIds.Count == 0 ||
            _operationContext.VisibleOrganizationIds.Contains(e.OrganizationId));
    }
}
