using Bogus;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.ApiKeys;
using DevInstance.DevCoreApp.Shared.Model.ApiKeys;
using DevInstance.DevCoreApp.Shared.Model.Permissions;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Mocks.ApiKeys;

[BlazorServiceMock]
public class ApiKeyAdminServiceMock : IApiKeyAdminService
{
    private readonly List<ApiKeyItem> _keys;
    private readonly int delay = 500;

    public ApiKeyAdminServiceMock()
    {
        _keys = GenerateKeys();
    }

    private static List<ApiKeyItem> GenerateKeys()
    {
        var now = DateTime.UtcNow;
        var allPermissions = PermissionDefinitions.GetAll();

        return new List<ApiKeyItem>
        {
            new()
            {
                Id = IdGenerator.New(), Name = "CI/CD Pipeline",
                Prefix = "dca_a1b2", Scopes = allPermissions.Take(5).ToList(),
                ExpiresAt = now.AddDays(90), LastUsedAt = now.AddHours(-2),
                IsRevoked = false, CreatedByName = "John Doe",
                CreateDate = now.AddDays(-30)
            },
            new()
            {
                Id = IdGenerator.New(), Name = "Mobile App Integration",
                Prefix = "dca_c3d4", Scopes = allPermissions.Take(3).ToList(),
                ExpiresAt = null, LastUsedAt = now.AddDays(-1),
                IsRevoked = false, CreatedByName = "Jane Smith",
                CreateDate = now.AddDays(-60)
            },
            new()
            {
                Id = IdGenerator.New(), Name = "Legacy System (Deprecated)",
                Prefix = "dca_e5f6", Scopes = null,
                ExpiresAt = now.AddDays(-10), LastUsedAt = now.AddDays(-15),
                IsRevoked = true, RevokedAt = now.AddDays(-5),
                CreatedByName = "Admin User",
                CreateDate = now.AddDays(-120)
            },
            new()
            {
                Id = IdGenerator.New(), Name = "Reporting Service",
                Prefix = "dca_g7h8", Scopes = new List<string> { "System.AuditLog.View", "System.Jobs.View" },
                ExpiresAt = now.AddDays(180), LastUsedAt = null,
                IsRevoked = false, CreatedByName = "John Doe",
                CreateDate = now.AddDays(-7)
            },
            new()
            {
                Id = IdGenerator.New(), Name = "Partner API Access",
                Prefix = "dca_i9j0", Scopes = new List<string> { "Admin.Users.View" },
                ExpiresAt = now.AddDays(30), LastUsedAt = now.AddMinutes(-30),
                IsRevoked = false, CreatedByName = "Jane Smith",
                CreateDate = now.AddDays(-14)
            },
        };
    }

    public async Task<ServiceActionResult<ModelList<ApiKeyItem>>> GetKeysAsync(
        int top, int page, string[]? sortBy = null, string? search = null)
    {
        await Task.Delay(delay);

        IEnumerable<ApiKeyItem> query = _keys;

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(k =>
                k.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                k.Prefix.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = query.ToList();
        var items = filtered.Skip(page * top).Take(top).ToArray();

        return ServiceActionResult<ModelList<ApiKeyItem>>.OK(
            ModelListResult.CreateList(items, filtered.Count, top, page, sortBy, search));
    }

    public async Task<ServiceActionResult<ApiKeyCreateResult>> CreateKeyAsync(ApiKeyItem item)
    {
        await Task.Delay(delay);

        var plainTextKey = "dca_" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")[..8];
        var prefix = plainTextKey[..8];

        item.Id = IdGenerator.New();
        item.Prefix = prefix;
        item.CreateDate = DateTime.UtcNow;
        item.CreatedByName = "Current User";
        _keys.Insert(0, item);

        var result = new ApiKeyCreateResult
        {
            Key = item,
            PlainTextKey = plainTextKey
        };

        return ServiceActionResult<ApiKeyCreateResult>.OK(result);
    }

    public async Task<ServiceActionResult<bool>> RevokeKeyAsync(string id)
    {
        await Task.Delay(delay);

        var item = _keys.Find(k => k.Id == id);
        if (item == null)
            throw new InvalidOperationException("API key not found.");

        item.IsRevoked = true;
        item.RevokedAt = DateTime.UtcNow;

        return ServiceActionResult<bool>.OK(true);
    }
}
