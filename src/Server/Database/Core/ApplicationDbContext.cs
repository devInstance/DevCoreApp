using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Core.Models.BackgroundTasks;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Files;
using DevInstance.DevCoreApp.Server.Database.Core.Models.ImportExport;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Notifications;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Webhooks;
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
    public DbSet<ApplicationLog> ApplicationLogs { get; set; }
    public DbSet<BackgroundTask> BackgroundTasks { get; set; }
    public DbSet<BackgroundTaskLog> BackgroundTaskLogs { get; set; }
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<UserNotificationPreference> UserNotificationPreferences { get; set; }
    public DbSet<FileRecord> FileRecords { get; set; }
    public DbSet<ImportSession> ImportSessions { get; set; }
    public DbSet<FeatureFlag> FeatureFlags { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; }
    public DbSet<WebhookDelivery> WebhookDeliveries { get; set; }

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

        builder.Entity<UserProfile>(entity =>
        {
            entity.Property(up => up.ProfilePicture)
                .HasColumnType("bytea");

            entity.Property(up => up.ProfilePictureThumbnail)
                .HasColumnType("bytea");
        });

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

        builder.Entity<ApplicationLog>(entity =>
        {
            entity.Property(al => al.Properties)
                .HasColumnType("jsonb");

            entity.HasIndex(al => al.Timestamp);

            entity.HasIndex(al => al.Level);

            entity.HasIndex(al => al.CorrelationId);

            entity.HasIndex(al => al.UserId);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(a => new { a.TableName, a.RecordId, a.ChangedAt });
        });

        builder.Entity<BackgroundTask>(entity =>
        {
            entity.Property(bt => bt.Payload)
                .HasColumnType("jsonb");

            entity.HasOne(bt => bt.CreatedBy)
                .WithMany()
                .HasForeignKey(bt => bt.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(bt => bt.Organization)
                .WithMany()
                .HasForeignKey(bt => bt.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(bt => bt.Status);
            entity.HasIndex(bt => bt.TaskType);
            entity.HasIndex(bt => bt.ScheduledAt);
            entity.HasIndex(bt => bt.OrganizationId);
        });

        builder.Entity<BackgroundTaskLog>(entity =>
        {
            entity.HasOne(btl => btl.BackgroundTask)
                .WithMany()
                .HasForeignKey(btl => btl.BackgroundTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(btl => btl.BackgroundTaskId);
        });

        builder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasIndex(nt => nt.Name)
                .IsUnique();
        });

        builder.Entity<Notification>(entity =>
        {
            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(n => n.Organization)
                .WithMany()
                .HasForeignKey(n => n.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(n => n.UserProfileId);
            entity.HasIndex(n => n.IsRead);
            entity.HasIndex(n => n.CreateDate);
            entity.HasIndex(n => n.GroupKey);
            entity.HasIndex(n => n.OrganizationId);
        });

        builder.Entity<UserNotificationPreference>(entity =>
        {
            entity.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Organization)
                .WithMany()
                .HasForeignKey(p => p.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(p => new { p.UserProfileId, p.NotificationCategory })
                .IsUnique();

            entity.HasIndex(p => p.OrganizationId);
        });

        builder.Entity<FileRecord>(entity =>
        {
            entity.HasOne(fr => fr.CreatedBy)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(fr => fr.UpdatedBy)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(fr => fr.Organization)
                .WithMany()
                .HasForeignKey(fr => fr.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(fr => fr.OrganizationId);
            entity.HasIndex(fr => new { fr.EntityType, fr.EntityId });
            entity.HasIndex(fr => fr.StorageProvider);
        });

        builder.Entity<ImportSession>(entity =>
        {
            entity.Property(s => s.ColumnMappingJson)
                .HasColumnType("jsonb");

            entity.Property(s => s.ValidationResultJson)
                .HasColumnType("jsonb");

            entity.HasOne(s => s.CreatedBy)
                .WithMany()
                .HasForeignKey(s => s.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Organization)
                .WithMany()
                .HasForeignKey(s => s.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(s => s.EntityType);
            entity.HasIndex(s => s.Status);
            entity.HasIndex(s => s.OrganizationId);
        });

        builder.Entity<FeatureFlag>(entity =>
        {
            entity.Property(ff => ff.AllowedUsers)
                .HasColumnType("jsonb");

            entity.HasOne(ff => ff.Organization)
                .WithMany()
                .HasForeignKey(ff => ff.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(ff => new { ff.Name, ff.OrganizationId })
                .IsUnique();

            entity.HasIndex(ff => ff.Name);
        });

        builder.Entity<ApiKey>(entity =>
        {
            entity.Property(ak => ak.Scopes)
                .HasColumnType("jsonb");

            entity.HasOne(ak => ak.CreatedBy)
                .WithMany()
                .HasForeignKey(ak => ak.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ak => ak.Organization)
                .WithMany()
                .HasForeignKey(ak => ak.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(ak => ak.KeyHash)
                .IsUnique();

            entity.HasIndex(ak => ak.Prefix);
        });

        builder.Entity<WebhookSubscription>(entity =>
        {
            entity.HasOne(ws => ws.CreatedBy)
                .WithMany()
                .HasForeignKey(ws => ws.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ws => ws.Organization)
                .WithMany()
                .HasForeignKey(ws => ws.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(ws => ws.EventType);
            entity.HasIndex(ws => new { ws.EventType, ws.IsActive });
        });

        builder.Entity<WebhookDelivery>(entity =>
        {
            entity.Property(wd => wd.Payload)
                .HasColumnType("jsonb");

            entity.HasOne(wd => wd.Subscription)
                .WithMany()
                .HasForeignKey(wd => wd.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(wd => wd.SubscriptionId);
            entity.HasIndex(wd => wd.Status);
            entity.HasIndex(wd => wd.EventType);
            entity.HasIndex(wd => wd.NextRetryAt);
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
