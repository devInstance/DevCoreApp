using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Organizations;
using DevInstance.WebServiceToolkit.Common.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Organizations;

public interface IOrganizationService
{
    Task<ServiceActionResult<ModelList<OrganizationItem>>> GetAllAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null,
        string? search = null, bool? isActive = null);

    Task<ServiceActionResult<List<OrganizationItem>>> GetTreeAsync();

    Task<ServiceActionResult<OrganizationItem>> GetAsync(string publicId);

    Task<ServiceActionResult<OrganizationItem>> CreateAsync(OrganizationItem item, string? parentPublicId);

    Task<ServiceActionResult<OrganizationItem>> UpdateAsync(string publicId, OrganizationItem item);

    Task<ServiceActionResult<OrganizationItem>> ToggleActiveAsync(string publicId);

    Task<ServiceActionResult<OrganizationItem>> MoveAsync(string publicId, string newParentPublicId);
}
