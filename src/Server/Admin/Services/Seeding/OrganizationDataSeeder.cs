using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.WebServiceToolkit.Common.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Seeding;

/// <summary>
/// Seeds the default Tenant and root Organization on first startup.
/// In Development environment, also creates sample child organizations.
/// All operations are idempotent — safe to run on every startup.
/// </summary>
public class OrganizationDataSeeder : IDataSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly IHostEnvironment _env;

    public int Order => 10;

    public OrganizationDataSeeder(ApplicationDbContext db, IHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Check if default tenant already exists
        var existingTenant = await _db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Subdomain == "default", cancellationToken);

        if (existingTenant != null)
            return;

        var now = DateTime.UtcNow;

        // 1. Create root organization
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

        // 2. Create default tenant linked to root organization
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

        // 3. In development, create sample child organizations
        if (_env.IsDevelopment())
        {
            SeedDevelopmentOrganizations(rootOrg, now);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private void SeedDevelopmentOrganizations(Organization root, DateTime now)
    {
        var eastRegion = CreateChildOrganization(root, "East Region", "EAST", "Region", 1, now);
        var westRegion = CreateChildOrganization(root, "West Region", "WEST", "Region", 2, now);

        CreateChildOrganization(eastRegion, "New York Office", "NYC", "Office", 1, now);
        CreateChildOrganization(eastRegion, "Boston Office", "BOS", "Office", 2, now);

        CreateChildOrganization(westRegion, "Los Angeles Office", "LA", "Office", 1, now);
        CreateChildOrganization(westRegion, "Seattle Office", "SEA", "Office", 2, now);
    }

    private Organization CreateChildOrganization(
        Organization parent, string name, string code, string type, int sortOrder, DateTime now)
    {
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            PublicId = IdGenerator.New(),
            Name = name,
            Code = code,
            ParentId = parent.Id,
            Level = parent.Level + 1,
            Path = $"{parent.Path}/{code}",
            Type = type,
            SortOrder = sortOrder,
            IsActive = true,
            CreateDate = now,
            UpdateDate = now
        };

        _db.Organizations.Add(org);
        return org;
    }
}
