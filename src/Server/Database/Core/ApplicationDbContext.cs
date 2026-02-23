using DevInstance.DevCoreApp.Server.Database.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace DevInstance.DevCoreApp.Server.Database.Core;

public abstract class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<GridProfile> GridProfiles { get; set; }
    public DbSet<EmailLog> EmailLogs { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<UserOrganization> UserOrganizations { get; set; }

    public ApplicationDbContext(DbContextOptions options)
            : base(options)
    {
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
    }
}
