using DevInstance.SampleWebApp.Server.Database.Core.Models;
using IdentityServer4.EntityFramework.Options;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;

namespace DevInstance.SampleWebApp.Server.Database.Core
{
    public abstract class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid> /*ApiAuthorizationDbContext<ApplicationUser>*/
    {
        public DbSet<UserProfile> UserProfiles { get; set; }

        public ApplicationDbContext(DbContextOptions options
            /*,IOptions<OperationalStoreOptions> operationalStoreOptions*/)
                : base(options/*, operationalStoreOptions*/)
        {
        }
    }
}
