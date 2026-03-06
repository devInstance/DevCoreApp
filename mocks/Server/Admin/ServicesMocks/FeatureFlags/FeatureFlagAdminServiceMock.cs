using Bogus;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.FeatureFlags;
using DevInstance.DevCoreApp.Shared.Model.FeatureFlags;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Mocks.FeatureFlags;

[BlazorServiceMock]
public class FeatureFlagAdminServiceMock : IFeatureFlagAdminService
{
    private readonly List<FeatureFlagItem> _flags;
    private readonly int delay = 500;

    public FeatureFlagAdminServiceMock()
    {
        _flags = GenerateFlags();
    }

    private static List<FeatureFlagItem> GenerateFlags()
    {
        var now = DateTime.UtcNow;

        return new List<FeatureFlagItem>
        {
            new()
            {
                Id = IdGenerator.New(), Name = "DarkMode", Description = "Enable dark mode theme for users",
                IsEnabled = true, RolloutPercentage = null, OrganizationName = null,
                CreateDate = now.AddDays(-30), UpdateDate = now.AddDays(-5)
            },
            new()
            {
                Id = IdGenerator.New(), Name = "NewDashboard", Description = "Redesigned dashboard with analytics widgets",
                IsEnabled = true, RolloutPercentage = 50, OrganizationName = null,
                CreateDate = now.AddDays(-14), UpdateDate = now.AddDays(-2)
            },
            new()
            {
                Id = IdGenerator.New(), Name = "BetaReports", Description = "Beta reporting module",
                IsEnabled = false, RolloutPercentage = null, OrganizationName = null,
                CreateDate = now.AddDays(-7), UpdateDate = now.AddDays(-7)
            },
            new()
            {
                Id = IdGenerator.New(), Name = "BulkExport", Description = "Allow bulk data export from grid views",
                IsEnabled = true, RolloutPercentage = null, OrganizationName = "East Region",
                CreateDate = now.AddDays(-21), UpdateDate = now.AddDays(-10)
            },
            new()
            {
                Id = IdGenerator.New(), Name = "AIAssist", Description = "AI-powered field suggestions",
                IsEnabled = true, RolloutPercentage = 25, OrganizationName = null,
                AllowedUsers = new List<string> { "user-001", "user-002" },
                CreateDate = now.AddDays(-3), UpdateDate = now.AddDays(-1)
            },
            new()
            {
                Id = IdGenerator.New(), Name = "TwoFactorEnforcement", Description = "Enforce 2FA for all users",
                IsEnabled = false, RolloutPercentage = null, OrganizationName = "West Region",
                CreateDate = now.AddDays(-60), UpdateDate = now.AddDays(-45)
            },
        };
    }

    public async Task<ServiceActionResult<ModelList<FeatureFlagItem>>> GetFlagsAsync(
        int top, int page, string[]? sortBy = null, string? search = null)
    {
        await Task.Delay(delay);

        IEnumerable<FeatureFlagItem> query = _flags;

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(f =>
                f.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (f.Description != null && f.Description.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        var filtered = query.ToList();
        var items = filtered.Skip(page * top).Take(top).ToArray();

        return ServiceActionResult<ModelList<FeatureFlagItem>>.OK(
            ModelListResult.CreateList(items, filtered.Count, top, page, sortBy, search));
    }

    public async Task<ServiceActionResult<FeatureFlagItem>> GetFlagAsync(string id)
    {
        await Task.Delay(delay);

        var item = _flags.Find(f => f.Id == id);
        if (item == null)
            throw new InvalidOperationException("Feature flag not found.");

        return ServiceActionResult<FeatureFlagItem>.OK(item);
    }

    public async Task<ServiceActionResult<FeatureFlagItem>> CreateFlagAsync(FeatureFlagItem item)
    {
        await Task.Delay(delay);

        item.Id = IdGenerator.New();
        item.CreateDate = DateTime.UtcNow;
        item.UpdateDate = DateTime.UtcNow;
        _flags.Add(item);

        return ServiceActionResult<FeatureFlagItem>.OK(item);
    }

    public async Task<ServiceActionResult<FeatureFlagItem>> UpdateFlagAsync(string id, FeatureFlagItem item)
    {
        await Task.Delay(delay);

        var index = _flags.FindIndex(f => f.Id == id);
        if (index < 0)
            throw new InvalidOperationException("Feature flag not found.");

        var existing = _flags[index];
        existing.Name = item.Name;
        existing.Description = item.Description;
        existing.IsEnabled = item.IsEnabled;
        existing.RolloutPercentage = item.RolloutPercentage;
        existing.AllowedUsers = item.AllowedUsers;
        existing.UpdateDate = DateTime.UtcNow;

        return ServiceActionResult<FeatureFlagItem>.OK(existing);
    }

    public async Task<ServiceActionResult<bool>> DeleteFlagAsync(string id)
    {
        await Task.Delay(delay);

        var item = _flags.Find(f => f.Id == id);
        if (item == null)
            throw new InvalidOperationException("Feature flag not found.");

        _flags.Remove(item);

        return ServiceActionResult<bool>.OK(true);
    }
}
