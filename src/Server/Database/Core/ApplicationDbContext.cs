using DevInstance.SampleWebApp.Server.Database.Core.Models;
using IdentityServer4.EntityFramework.Options;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevInstance.SampleWebApp.Server.Database.Core
{
    public abstract class ApplicationDbContext : ApiAuthorizationDbContext<ApplicationUser>
    {
        public DbSet<UserProfile> UserProfiles { get; set; }

        public ApplicationDbContext(DbContextOptions options,
                                    IOptions<OperationalStoreOptions> operationalStoreOptions)
                : base(options, operationalStoreOptions)
        {
        }
    }
}
