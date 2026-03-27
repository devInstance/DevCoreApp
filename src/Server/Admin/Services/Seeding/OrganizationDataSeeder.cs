using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.WebServiceToolkit.Common.Tools;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Seeding;

/// <summary>
/// Seeds the default Tenant and root Organization on first startup.
/// All operations are idempotent — safe to run on every startup.
/// </summary>
public class OrganizationDataSeeder : IDataSeeder
{
    private readonly ApplicationDbContext _db;

    public int Order => 10;

    public OrganizationDataSeeder(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingTenant = await _db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Subdomain == "default", cancellationToken);

        if (existingTenant != null)
            return;

        var now = DateTime.UtcNow;

        var rootOrg = new Organization
        {
            Id = Guid.NewGuid(),
            PublicId = IdGenerator.New(),
            Name = "Default Organization",
            Code = "DEFAULT",
            ParentId = null,
            Level = 0,
            Path = "/DEFAULT",
            Type = "Root",
            SortOrder = 0,
            IsActive = true,
            CreateDate = now,
            UpdateDate = now
        };

        _db.Organizations.Add(rootOrg);

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            PublicId = IdGenerator.New(),
            Name = "Default Tenant",
            Subdomain = "default",
            Status = TenantStatus.Active,
            Plan = "Standard",
            RootOrganizationId = rootOrg.Id,
            IsActive = true,
            CreateDate = now,
            UpdateDate = now
        };

        _db.Tenants.Add(tenant);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
