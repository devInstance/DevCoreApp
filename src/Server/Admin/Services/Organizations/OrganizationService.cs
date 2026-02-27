using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Admin.Services.Exceptions;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Model.Organizations;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;
using DevInstance.WebServiceToolkit.Database.Queries.Extensions;
using DevInstance.WebServiceToolkit.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Organizations;

[BlazorService]
public class OrganizationService : BaseService, IOrganizationService
{
    private readonly IScopeLog log;

    public OrganizationService(IScopeManager logManager,
                               ITimeProvider timeProvider,
                               IQueryRepository query,
                               IAuthorizationContext authorizationContext)
        : base(logManager, timeProvider, query, authorizationContext)
    {
        log = logManager.CreateLogger(this);
    }

    public async Task<ServiceActionResult<ModelList<OrganizationItem>>> GetAllAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null,
        string? search = null, bool? isActive = null)
    {
        using var l = log.TraceScope();

        var query = Repository.GetOrganizationsQuery(AuthorizationContext.CurrentProfile);

        if (!string.IsNullOrEmpty(search))
            query = query.Search(search);

        if (isActive.HasValue)
            query = query.ByIsActive(isActive.Value);

        query = !string.IsNullOrEmpty(sortField)
            ? query.SortBy(sortField, isAsc ?? true)
            : query.SortBy("Path", true);

        var totalCount = await query.Clone().Select().CountAsync();
        var orgs = await query.Paginate(top, page).Select()
            .Include(o => o.Parent)
            .ToListAsync();

        var items = orgs.Select(o => o.ToView()).ToArray();

        string[]? sortBy = !string.IsNullOrEmpty(sortField)
            ? new[] { (isAsc == false ? "-" : "") + sortField }
            : null;

        var modelList = ModelListResult.CreateList(items, totalCount, top, page, sortBy, search);
        return ServiceActionResult<ModelList<OrganizationItem>>.OK(modelList);
    }

    public async Task<ServiceActionResult<List<OrganizationItem>>> GetTreeAsync()
    {
        using var l = log.TraceScope();

        var orgs = await Repository.GetOrganizationsQuery(AuthorizationContext.CurrentProfile)
            .SortBy("Path", true)
            .Select()
            .Include(o => o.Parent)
            .ToListAsync();

        var items = orgs.Select(o => o.ToView()).ToList();
        return ServiceActionResult<List<OrganizationItem>>.OK(items);
    }

    public async Task<ServiceActionResult<OrganizationItem>> GetAsync(string publicId)
    {
        using var l = log.TraceScope();

        var org = await Repository.GetOrganizationsQuery(AuthorizationContext.CurrentProfile)
            .ByPublicId(publicId)
            .Select()
            .Include(o => o.Parent)
            .FirstOrDefaultAsync();

        if (org == null)
            throw new RecordNotFoundException("Organization not found.");

        return ServiceActionResult<OrganizationItem>.OK(org.ToView());
    }

    public async Task<ServiceActionResult<OrganizationItem>> CreateAsync(OrganizationItem item, string? parentPublicId)
    {
        using var l = log.TraceScope();

        var query = Repository.GetOrganizationsQuery(AuthorizationContext.CurrentProfile);

        // Validate code uniqueness
        var existing = await query.Clone().ByCode(item.Code).Select().FirstOrDefaultAsync();
        if (existing != null)
            throw new RecordConflictException($"An organization with code '{item.Code}' already exists.");

        Organization? parent = null;
        if (!string.IsNullOrEmpty(parentPublicId))
        {
            parent = await query.Clone().ByPublicId(parentPublicId).Select().FirstOrDefaultAsync();
            if (parent == null)
                throw new RecordNotFoundException("Parent organization not found.");
        }

        var org = query.CreateNew();
        org.Name = item.Name;
        org.Code = item.Code;
        org.Type = item.Type;
        org.SortOrder = item.SortOrder;
        org.Settings = item.Settings;
        org.IsActive = true;

        if (parent != null)
        {
            org.ParentId = parent.Id;
            org.Level = parent.Level + 1;
            org.Path = $"{parent.Path}/{item.Code}";
        }
        else
        {
            org.Level = 0;
            org.Path = $"/{item.Code}";
        }

        await query.AddAsync(org);

        l.I($"Organization created: {org.Name} ({org.Code}) at {org.Path}");
        return ServiceActionResult<OrganizationItem>.OK(org.ToView());
    }

    public async Task<ServiceActionResult<OrganizationItem>> UpdateAsync(string publicId, OrganizationItem item)
    {
        using var l = log.TraceScope();

        var query = Repository.GetOrganizationsQuery(AuthorizationContext.CurrentProfile);
        var org = await query.ByPublicId(publicId).Select()
            .Include(o => o.Parent)
            .FirstOrDefaultAsync();

        if (org == null)
            throw new RecordNotFoundException("Organization not found.");

        // If code changed, check uniqueness and recompute paths
        var codeChanged = !string.Equals(org.Code, item.Code, StringComparison.Ordinal);
        if (codeChanged)
        {
            var duplicate = await Repository.GetOrganizationsQuery(AuthorizationContext.CurrentProfile)
                .ByCode(item.Code).Select().FirstOrDefaultAsync();
            if (duplicate != null && duplicate.Id != org.Id)
                throw new RecordConflictException($"An organization with code '{item.Code}' already exists.");
        }

        var oldPath = org.Path;
        org.Name = item.Name;
        org.Code = item.Code;
        org.Type = item.Type;
        org.SortOrder = item.SortOrder;
        org.Settings = item.Settings;

        if (codeChanged)
        {
            var parentPath = org.Parent != null ? org.Parent.Path : string.Empty;
            org.Path = $"{parentPath}/{item.Code}";
            await query.UpdateAsync(org);
            await RecomputeSubtreePathsAsync(oldPath, org.Path);
        }
        else
        {
            await query.UpdateAsync(org);
        }

        l.I($"Organization updated: {org.Name} ({org.Code})");
        return ServiceActionResult<OrganizationItem>.OK(org.ToView());
    }

    public async Task<ServiceActionResult<OrganizationItem>> ToggleActiveAsync(string publicId)
    {
        using var l = log.TraceScope();

        var query = Repository.GetOrganizationsQuery(AuthorizationContext.CurrentProfile);
        var org = await query.ByPublicId(publicId).Select().FirstOrDefaultAsync();

        if (org == null)
            throw new RecordNotFoundException("Organization not found.");

        if (org.Level == 0)
            throw new BusinessRuleException("Cannot deactivate the root organization.");

        org.IsActive = !org.IsActive;
        await query.UpdateAsync(org);

        l.I($"Organization {(org.IsActive ? "activated" : "deactivated")}: {org.Name}");
        return ServiceActionResult<OrganizationItem>.OK(org.ToView());
    }

    public async Task<ServiceActionResult<OrganizationItem>> MoveAsync(string publicId, string newParentPublicId)
    {
        using var l = log.TraceScope();

        var query = Repository.GetOrganizationsQuery(AuthorizationContext.CurrentProfile);
        var org = await query.ByPublicId(publicId).Select().FirstOrDefaultAsync();

        if (org == null)
            throw new RecordNotFoundException("Organization not found.");

        if (org.Level == 0)
            throw new BusinessRuleException("Cannot move the root organization.");

        var newParent = await Repository.GetOrganizationsQuery(AuthorizationContext.CurrentProfile)
            .ByPublicId(newParentPublicId).Select().FirstOrDefaultAsync();

        if (newParent == null)
            throw new RecordNotFoundException("Target parent organization not found.");

        // Prevent moving into own subtree
        if (newParent.Path.StartsWith(org.Path + "/") || newParent.Id == org.Id)
            throw new BusinessRuleException("Cannot move an organization into its own subtree.");

        var oldPath = org.Path;
        org.ParentId = newParent.Id;
        org.Level = newParent.Level + 1;
        org.Path = $"{newParent.Path}/{org.Code}";

        await query.UpdateAsync(org);
        await RecomputeSubtreePathsAsync(oldPath, org.Path);

        l.I($"Organization moved: {org.Name} to {newParent.Path}");
        return ServiceActionResult<OrganizationItem>.OK(org.ToView());
    }

    private async Task RecomputeSubtreePathsAsync(string oldPathPrefix, string newPathPrefix)
    {
        // Find all descendants whose path starts with the old prefix
        var descendants = await Repository.GetOrganizationsQuery(AuthorizationContext.CurrentProfile)
            .ByPathPrefix(oldPathPrefix + "/")
            .Select()
            .ToListAsync();

        foreach (var descendant in descendants)
        {
            descendant.Path = newPathPrefix + descendant.Path.Substring(oldPathPrefix.Length);
            // Recompute level from path segments: /A/B/C → Level 2
            descendant.Level = descendant.Path.Count(c => c == '/') - 1;
        }

        if (descendants.Count > 0)
        {
            // Batch update
            foreach (var descendant in descendants)
            {
                var updateQuery = Repository.GetOrganizationsQuery(AuthorizationContext.CurrentProfile);
                await updateQuery.UpdateAsync(descendant);
            }
        }
    }
}
