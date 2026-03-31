using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.WebServiceToolkit.Common.Tools;
using Microsoft.EntityFrameworkCore;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Seeding;

/// <summary>
/// Seeds default database-backed settings that are already consumed by runtime services.
/// Idempotent and safe to run on every startup.
/// </summary>
public class SettingsDataSeeder : IDataSeeder
{
    private readonly ApplicationDbContext _db;

    public int Order => 15;

    public SettingsDataSeeder(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSystemSettingAsync(
            "Storage",
            "AllowedContentTypes",
            "\"*\"",
            "string",
            "Comma-separated list of allowed content types for uploads. Use * to allow all types.",
            cancellationToken);

        await EnsureSystemSettingAsync(
            "Storage",
            "MaxFileSizeBytes",
            "10485760",
            "int",
            "Maximum allowed upload size in bytes.",
            cancellationToken);

        await EnsureSystemSettingAsync(
            "Storage",
            "SoftDelete",
            "false",
            "bool",
            "When true, deleting a file only deactivates the record instead of removing storage immediately.",
            cancellationToken);

        await EnsureSystemSettingAsync(
            "Appearance",
            "Theme",
            "\"System\"",
            "string",
            "Default application theme preference when no user-specific value exists.",
            cancellationToken);
    }

    private async Task EnsureSystemSettingAsync(
        string category,
        string key,
        string value,
        string valueType,
        string description,
        CancellationToken cancellationToken)
    {
        var existing = await _db.Settings
            .FirstOrDefaultAsync(
                s => s.TenantId == null &&
                     s.OrganizationId == null &&
                     s.UserId == null &&
                     s.Category == category &&
                     s.Key == key,
                cancellationToken);

        if (existing != null)
            return;

        var now = DateTime.UtcNow;
        _db.Settings.Add(new Setting
        {
            Id = Guid.NewGuid(),
            Category = category,
            Key = key,
            Value = value,
            ValueType = valueType,
            Description = description,
            PublicId = IdGenerator.New(),
            CreateDate = now,
            UpdateDate = now,
            IsActive = true
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
