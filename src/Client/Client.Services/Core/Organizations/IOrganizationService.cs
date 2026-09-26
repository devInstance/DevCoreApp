using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Organizations;

namespace DevInstance.DevCoreApp.Client.Services.Core.Organizations;

/// <summary>Organization tree (<c>api/organizations</c>). Same shape as the server service.</summary>
public interface IOrganizationService
{
    Task<ServiceActionResult<PagedList<OrganizationItem>>> GetAllAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null,
        string? search = null, bool? isActive = null);

    /// <summary>Visible organizations as a flat list ordered by path (Level = depth).</summary>
    Task<ServiceActionResult<List<OrganizationItem>>> GetTreeAsync();

    Task<ServiceActionResult<OrganizationItem>> GetAsync(string publicId);

    /// <summary>The caller's primary organization, or null.</summary>
    Task<ServiceActionResult<OrganizationItem?>> GetCurrentAsync();

    Task<ServiceActionResult<OrganizationItem>> CreateAsync(OrganizationItem item, string? parentPublicId);
    Task<ServiceActionResult<OrganizationItem>> UpdateAsync(string publicId, OrganizationItem item);
    Task<ServiceActionResult<OrganizationItem>> ToggleActiveAsync(string publicId);
    Task<ServiceActionResult<OrganizationItem>> MoveAsync(string publicId, string newParentPublicId);
}
