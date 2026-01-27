using DevInstance.DevCoreApp.Server.Database.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace DevInstance.DevCoreApp.Server.Database.Core;

public abstract class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public DbSet<UserProfile> UserProfiles { get; set; }

    public ApplicationDbContext(DbContextOptions options)
            : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
}
