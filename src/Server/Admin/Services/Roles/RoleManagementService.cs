using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Exceptions;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Model.Roles;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;
using DevInstance.WebServiceToolkit.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Roles;

[BlazorService]
public class RoleManagementService : IRoleManagementService
{
    private readonly IScopeLog log;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IQueryRepository _repository;

    public RoleManagementService(
        IScopeManager logManager,
        RoleManager<ApplicationRole> roleManager,
        IQueryRepository repository)
    {
        log = logManager.CreateLogger(this);
        _roleManager = roleManager;
        _repository = repository;
    }

    public async Task<ServiceActionResult<ModelList<RoleItem>>> GetRolesAsync(int? top, int? page, string[]? sortBy, string? search)
    {
        using var l = log.TraceScope();

        var query = _roleManager.Roles;

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(r =>
                r.Name!.Contains(search) ||
                (r.Description != null && r.Description.Contains(search)));
        }

        var sortField = sortBy?.FirstOrDefault()?.TrimStart('-');
        var isAsc = sortBy?.FirstOrDefault()?.StartsWith("-") != true;

        query = sortField?.ToLowerInvariant() switch
        {
            "name" => isAsc ? query.OrderBy(r => r.Name) : query.OrderByDescending(r => r.Name),
            "description" => isAsc ? query.OrderBy(r => r.Description) : query.OrderByDescending(r => r.Description),
            _ => query.OrderBy(r => r.Name)
        };

        var totalCount = await query.CountAsync();

        var topVal = top ?? 10;
        var pageVal = page ?? 0;
        var roles = await query.Skip(pageVal * topVal).Take(topVal).ToListAsync();

        var roleIds = roles.Select(r => r.Id).ToList();
        var permissionCounts = await _repository.GetRolePermissionQuery(null!)
            .CountByRoleIdsAsync(roleIds);

        var items = roles.Select(r => new RoleItem
        {
            Id = r.Id.ToString(),
            Name = r.Name ?? string.Empty,
            Description = r.Description,
            IsSystemRole = r.IsSystemRole,
            PermissionCount = permissionCounts.GetValueOrDefault(r.Id, 0)
        }).ToArray();

        var modelList = ModelListResult.CreateList(items, totalCount, topVal, pageVal, sortBy, search);
        return ServiceActionResult<ModelList<RoleItem>>.OK(modelList);
    }

    public async Task<ServiceActionResult<RoleItem>> GetRoleAsync(string roleId)
    {
        using var l = log.TraceScope();

        if (!Guid.TryParse(roleId, out var guid))
            throw new RecordNotFoundException("Role not found.");

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
            throw new RecordNotFoundException("Role not found.");

        var permissionCount = await _repository.GetRolePermissionQuery(null!).CountForRoleIdAsync(guid);

        return ServiceActionResult<RoleItem>.OK(new RoleItem
        {
            Id = role.Id.ToString(),
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            PermissionCount = permissionCount
        });
    }

    public async Task<ServiceActionResult<RoleItem>> CreateRoleAsync(RoleItem item)
    {
        using var l = log.TraceScope();

        var existing = await _roleManager.FindByNameAsync(item.Name);
        if (existing != null)
            throw new RecordConflictException($"A role with name '{item.Name}' already exists.");

        var role = new ApplicationRole(item.Name)
        {
            Description = item.Description,
            IsSystemRole = false
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
            throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));

        l.I($"Role created: {role.Name}");

        return ServiceActionResult<RoleItem>.OK(new RoleItem
        {
            Id = role.Id.ToString(),
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            IsSystemRole = false,
            PermissionCount = 0
        });
    }

    public async Task<ServiceActionResult<RoleItem>> UpdateRoleAsync(string roleId, RoleItem item)
    {
        using var l = log.TraceScope();

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
            throw new RecordNotFoundException("Role not found.");

        if (role.IsSystemRole)
        {
            // System roles: only description can be edited
            role.Description = item.Description;
        }
        else
        {
            // Check name uniqueness if changed
            if (!string.Equals(role.Name, item.Name, StringComparison.OrdinalIgnoreCase))
            {
                var duplicate = await _roleManager.FindByNameAsync(item.Name);
                if (duplicate != null && duplicate.Id != role.Id)
                    throw new RecordConflictException($"A role with name '{item.Name}' already exists.");

                role.Name = item.Name;
            }

            role.Description = item.Description;
        }

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
            throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));

        var permissionCount = await _repository.GetRolePermissionQuery(null!).CountForRoleIdAsync(role.Id);

        l.I($"Role updated: {role.Name}");

        return ServiceActionResult<RoleItem>.OK(new RoleItem
        {
            Id = role.Id.ToString(),
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            PermissionCount = permissionCount
        });
    }

    public async Task<ServiceActionResult<bool>> DeleteRoleAsync(string roleId)
    {
        using var l = log.TraceScope();

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
            throw new RecordNotFoundException("Role not found.");

        if (role.IsSystemRole)
            throw new BusinessRuleException("System roles cannot be deleted.");

        var hasUsers = await _repository.GetRolePermissionQuery(null!).RoleHasUsersAsync(role.Id);
        if (hasUsers)
            throw new BusinessRuleException("Cannot delete a role that has users assigned to it. Remove all users from this role first.");

        // Remove role permissions first
        await _repository.GetRolePermissionQuery(null!).RemoveAllForRoleAsync(role.Id);

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));

        l.I($"Role deleted: {role.Name}");

        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<List<PermissionItem>>> GetAllPermissionsAsync()
    {
        using var l = log.TraceScope();

        var permissions = await _repository.GetPermissionQuery(null!)
            .OrderedByDisplayOrder()
            .Select()
            .ToListAsync();

        var items = permissions.Select(p => new PermissionItem
        {
            Id = p.Id.ToString(),
            Module = p.Module,
            Entity = p.Entity,
            Action = p.Action,
            Key = p.Key,
            Description = p.Description,
            DisplayOrder = p.DisplayOrder
        }).ToList();

        return ServiceActionResult<List<PermissionItem>>.OK(items);
    }

    public async Task<ServiceActionResult<List<string>>> GetRolePermissionKeysAsync(string roleId)
    {
        using var l = log.TraceScope();

        if (!Guid.TryParse(roleId, out var guid))
            throw new RecordNotFoundException("Role not found.");

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
            throw new RecordNotFoundException("Role not found.");

        var keys = await _repository.GetRolePermissionQuery(null!).GetPermissionKeysForRoleIdAsync(guid);

        return ServiceActionResult<List<string>>.OK(keys.ToList());
    }

    public async Task<ServiceActionResult<bool>> SetRolePermissionsAsync(string roleId, RolePermissionsRequest request)
    {
        using var l = log.TraceScope();

        if (!Guid.TryParse(roleId, out var guid))
            throw new RecordNotFoundException("Role not found.");

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
            throw new RecordNotFoundException("Role not found.");

        if (role.IsSystemRole)
            throw new BusinessRuleException("Permissions for system roles are managed automatically and cannot be modified.");

        // Full replace: remove existing, add new
        await _repository.GetRolePermissionQuery(null!)
            .ReplaceRolePermissionsAsync(guid, request.PermissionKeys);

        l.I($"Role permissions updated for: {role.Name} ({request.PermissionKeys.Count} permissions)");

        return ServiceActionResult<bool>.OK(true);
    }
}
