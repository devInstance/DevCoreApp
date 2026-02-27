using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Exceptions;
using DevInstance.DevCoreApp.Server.Database.Core;
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
    private readonly ApplicationDbContext _db;

    public RoleManagementService(
        IScopeManager logManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext db)
    {
        log = logManager.CreateLogger(this);
        _roleManager = roleManager;
        _db = db;
    }

    public async Task<ServiceActionResult<ModelList<RoleItem>>> GetRolesAsync(int? top, int? page, string[]? sortBy, string? search)
    {
        using var l = log.TraceScope();

        var query = _db.Roles.AsQueryable();

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
        var permissionCounts = await _db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .GroupBy(rp => rp.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count);

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

        var permissionCount = await _db.RolePermissions.CountAsync(rp => rp.RoleId == guid);

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

        var permissionCount = await _db.RolePermissions.CountAsync(rp => rp.RoleId == role.Id);

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

        var hasUsers = await _db.UserRoles.AnyAsync(ur => ur.RoleId == role.Id);
        if (hasUsers)
            throw new BusinessRuleException("Cannot delete a role that has users assigned to it. Remove all users from this role first.");

        // Remove role permissions first
        var rolePermissions = await _db.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .ToListAsync();
        _db.RolePermissions.RemoveRange(rolePermissions);
        await _db.SaveChangesAsync();

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));

        l.I($"Role deleted: {role.Name}");

        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<List<PermissionItem>>> GetAllPermissionsAsync()
    {
        using var l = log.TraceScope();

        var permissions = await _db.Permissions
            .OrderBy(p => p.DisplayOrder)
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

        var keys = await _db.RolePermissions
            .Where(rp => rp.RoleId == guid)
            .Join(_db.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p.Key)
            .ToListAsync();

        return ServiceActionResult<List<string>>.OK(keys);
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
        var existingMappings = await _db.RolePermissions
            .Where(rp => rp.RoleId == guid)
            .ToListAsync();
        _db.RolePermissions.RemoveRange(existingMappings);

        if (request.PermissionKeys.Count > 0)
        {
            var permissionsByKey = await _db.Permissions
                .Where(p => request.PermissionKeys.Contains(p.Key))
                .ToDictionaryAsync(p => p.Key, p => p.Id);

            foreach (var key in request.PermissionKeys)
            {
                if (permissionsByKey.TryGetValue(key, out var permissionId))
                {
                    _db.RolePermissions.Add(new RolePermission
                    {
                        RoleId = guid,
                        PermissionId = permissionId
                    });
                }
            }
        }

        await _db.SaveChangesAsync();

        l.I($"Role permissions updated for: {role.Name} ({request.PermissionKeys.Count} permissions)");

        return ServiceActionResult<bool>.OK(true);
    }
}
