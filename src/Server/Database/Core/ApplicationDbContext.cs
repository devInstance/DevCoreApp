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
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserPermissionOverride> UserPermissionOverrides { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<UserLoginHistory> UserLoginHistories { get; set; }
    public DbSet<Setting> Settings { get; set; }

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

        builder.Entity<Permission>(entity =>
        {
            entity.HasIndex(p => p.Key)
                .IsUnique();

            entity.HasIndex(p => new { p.Module, p.Entity, p.Action })
                .IsUnique();
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            entity.HasOne(rp => rp.Role)
                .WithMany()
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserPermissionOverride>(entity =>
        {
            entity.HasOne(upo => upo.User)
                .WithMany()
                .HasForeignKey(upo => upo.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(upo => upo.Permission)
                .WithMany(p => p.UserPermissionOverrides)
                .HasForeignKey(upo => upo.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(upo => new { upo.UserId, upo.PermissionId })
                .IsUnique();
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(rt => rt.TokenHash)
                .IsUnique();

            entity.HasIndex(rt => rt.UserId);

            entity.HasIndex(rt => rt.ExpiresAt);
        });

        builder.Entity<UserLoginHistory>(entity =>
        {
            entity.HasOne(ulh => ulh.User)
                .WithMany()
                .HasForeignKey(ulh => ulh.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(ulh => ulh.UserId);

            entity.HasIndex(ulh => ulh.LoginAt);
        });

        builder.Entity<Setting>(entity =>
        {
            entity.HasOne(s => s.Tenant)
                .WithMany()
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Organization)
                .WithMany()
                .HasForeignKey(s => s.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => new { s.TenantId, s.OrganizationId, s.UserId, s.Category, s.Key })
                .IsUnique();

            entity.HasIndex(s => s.Category);
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
