using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Services.Core.Exceptions;
using DevInstance.DevCoreApp.Server.Services.Core.Organizations;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Organizations;
using DevInstance.WebServiceToolkit.Common.Tools;
using DevInstance.WebServiceToolkit.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Services.Mocks.Core.Organizations;

/// <summary>
/// In-memory organization tree: Acme Corp → East Region (New York, Boston) / West Region (San Francisco).
/// Like the real service, the "tree" is a flat list ordered by <see cref="OrganizationItem.Path"/>
/// with <see cref="OrganizationItem.Level"/> giving the depth.
/// </summary>
[BlazorServiceMock]
public class OrganizationServiceMock : IOrganizationService
{
    private readonly List<OrganizationItem> organizations = new();
    private readonly int delay = 300;

    public OrganizationServiceMock()
    {
        var root = Add("Acme Corp", "ACME", "Company", null);
        var east = Add("East Region", "EAST", "Region", root);
        Add("New York Office", "NYC", "Office", east);
        Add("Boston Office", "BOS", "Office", east);
        var west = Add("West Region", "WEST", "Region", root);
        Add("San Francisco Office", "SFO", "Office", west);
    }

    private OrganizationItem Add(string name, string code, string type, OrganizationItem? parent)
    {
        var now = DateTime.UtcNow;
        var item = new OrganizationItem
        {
            Id = IdGenerator.New(),
            Name = name,
            Code = code,
            Type = type,
            ParentId = parent?.Id,
            Level = parent == null ? 0 : parent.Level + 1,
            Path = (parent?.Path ?? "") + "/" + code,
            SortOrder = organizations.Count(o => o.ParentId == parent?.Id),
            CreateDate = now,
            UpdateDate = now
        };
        organizations.Add(item);
        return item;
    }

    private OrganizationItem Find(string publicId) =>
        organizations.FirstOrDefault(o => o.Id == publicId) ?? throw new RecordNotFoundException(publicId);

    public async Task<ServiceActionResult<PagedList<OrganizationItem>>> GetAllAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null, string? search = null, bool? isActive = null)
    {
        await Task.Delay(delay);

        IEnumerable<OrganizationItem> query = organizations;
        if (!string.IsNullOrEmpty(search))
            query = query.Where(o => o.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                                  || o.Code.Contains(search, StringComparison.OrdinalIgnoreCase));
        if (isActive.HasValue)
            query = query.Where(o => o.IsActive == isActive.Value);

        query = sortField?.ToLowerInvariant() switch
        {
            "name" => isAsc == false ? query.OrderByDescending(o => o.Name) : query.OrderBy(o => o.Name),
            "code" => isAsc == false ? query.OrderByDescending(o => o.Code) : query.OrderBy(o => o.Code),
            _ => query.OrderBy(o => o.Path)
        };

        var all = query.ToArray();
        var size = top ?? all.Length;
        var items = all.Skip((page ?? 0) * size).Take(size).ToArray();
        var sortOrder = sortField == null ? null : new[] { (isAsc == false ? "-" : "") + sortField };
        return ServiceActionResult<PagedList<OrganizationItem>>.OK(
            PagedList.Create(items, all.Length, top, page, sortOrder, search));
    }

    public async Task<ServiceActionResult<List<OrganizationItem>>> GetTreeAsync()
    {
        await Task.Delay(delay);
        return ServiceActionResult<List<OrganizationItem>>.OK(organizations.OrderBy(o => o.Path).ToList());
    }

    public async Task<ServiceActionResult<OrganizationItem>> GetAsync(string publicId)
    {
        await Task.Delay(delay);
        return ServiceActionResult<OrganizationItem>.OK(Find(publicId));
    }

    public async Task<ServiceActionResult<OrganizationItem?>> GetCurrentAsync()
    {
        await Task.Delay(delay);
        return ServiceActionResult<OrganizationItem?>.OK(organizations.First(o => o.ParentId == null));
    }

    public async Task<ServiceActionResult<OrganizationItem>> CreateAsync(OrganizationItem item, string? parentPublicId)
    {
        await Task.Delay(delay);
        var parent = parentPublicId == null ? null : Find(parentPublicId);
        var created = Add(item.Name, item.Code, item.Type, parent);
        return ServiceActionResult<OrganizationItem>.OK(created);
    }

    public async Task<ServiceActionResult<OrganizationItem>> UpdateAsync(string publicId, OrganizationItem item)
    {
        await Task.Delay(delay);
        var existing = Find(publicId);
        existing.Name = item.Name;
        existing.Code = item.Code;
        existing.Type = item.Type;
        existing.Settings = item.Settings;
        existing.UpdateDate = DateTime.UtcNow;
        return ServiceActionResult<OrganizationItem>.OK(existing);
    }

    public async Task<ServiceActionResult<OrganizationItem>> ToggleActiveAsync(string publicId)
    {
        await Task.Delay(delay);
        var existing = Find(publicId);
        existing.IsActive = !existing.IsActive;
        existing.UpdateDate = DateTime.UtcNow;
        return ServiceActionResult<OrganizationItem>.OK(existing);
    }

    public async Task<ServiceActionResult<OrganizationItem>> MoveAsync(string publicId, string newParentPublicId)
    {
        await Task.Delay(delay);
        var item = Find(publicId);
        var newParent = Find(newParentPublicId);

        if (newParent.Path == item.Path || newParent.Path.StartsWith(item.Path + "/"))
            throw new BusinessRuleException("An organization cannot be moved under itself or its own descendant.");

        var oldPath = item.Path;
        item.ParentId = newParent.Id;
        item.Path = newParent.Path + "/" + item.Code;
        var levelShift = newParent.Level + 1 - item.Level;
        item.Level += levelShift;

        foreach (var descendant in organizations.Where(o => o.Path.StartsWith(oldPath + "/")))
        {
            descendant.Path = item.Path + descendant.Path.Substring(oldPath.Length);
            descendant.Level += levelShift;
        }

        return ServiceActionResult<OrganizationItem>.OK(item);
    }
}
