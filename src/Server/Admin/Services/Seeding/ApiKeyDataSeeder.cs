using DevInstance.DevCoreApp.Server.Admin.Services.ApiKeys;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Seeding;

/// <summary>
/// Backfills legacy API keys that were created before scopes became a stable
/// permission snapshot. Keys with no scopes are assigned the owner's current
/// effective permissions once so runtime auth no longer drifts with later role changes.
/// </summary>
public class ApiKeyDataSeeder : IDataSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly IApiKeyPermissionSnapshotService _permissionSnapshotService;

    public int Order => 30;

    public ApiKeyDataSeeder(
        ApplicationDbContext db,
        IApiKeyPermissionSnapshotService permissionSnapshotService)
    {
        _db = db;
        _permissionSnapshotService = permissionSnapshotService;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var keysToBackfill = await _db.ApiKeys
            .Where(ak => ak.Scopes == null || ak.Scopes.Count == 0)
            .ToListAsync(cancellationToken);

        if (keysToBackfill.Count == 0)
            return;

        foreach (var apiKey in keysToBackfill)
        {
            if (!apiKey.CreatedById.HasValue)
            {
                apiKey.Scopes = new System.Collections.Generic.List<string>();
                continue;
            }

            apiKey.Scopes = await _permissionSnapshotService
                .GetEffectivePermissionsAsync(apiKey.CreatedById.Value, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
