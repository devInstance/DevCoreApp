using DevInstance.BlazorToolkit.Http;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Client.Services.Core.Time;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.LogScope;
using DevInstance.DevCoreApp.Shared.Model.Core.Organizations;

namespace DevInstance.DevCoreApp.Client.Services.Core.Organizations;

[BlazorService]
public class OrganizationService : ApiServiceBase, IOrganizationService
{
    public OrganizationService(IHttpApiContextFactory apiFactory, IScopeManager logManager) : base(apiFactory, logManager) { }

    public Task<ServiceActionResult<PagedList<OrganizationItem>>> GetAllAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null,
        string? search = null, bool? isActive = null) =>
        CallAsync(() => Api<OrganizationItem>("api/organizations").Get()
            .Query(new OrganizationQuery
            {
                Top = top ?? 20, Page = page ?? 0, SortBy = SortBy(sortField, isAsc)!, Search = search!, IsActive = isActive
            })
            .ExecuteAsync<PagedList<OrganizationItem>>());

    public Task<ServiceActionResult<List<OrganizationItem>>> GetTreeAsync() =>
        CallAsync(() => Api<List<OrganizationItem>>("api/organizations/tree").Get().ExecuteAsync());

    public Task<ServiceActionResult<OrganizationItem>> GetAsync(string publicId) =>
        CallAsync(() => Api<OrganizationItem>($"api/organizations/{Segment(publicId)}").Get().ExecuteAsync());

    public Task<ServiceActionResult<OrganizationItem?>> GetCurrentAsync() =>
        CallAsync<OrganizationItem?>(() => Api<OrganizationItem>("api/organizations/current").Get().ExecuteAsync());

    public Task<ServiceActionResult<OrganizationItem>> CreateAsync(OrganizationItem item, string? parentPublicId)
    {
        var api = Api<OrganizationItem>("api/organizations").Post(item);
        if (!string.IsNullOrEmpty(parentPublicId))
        {
            api = api.Parameter("parentId", Segment(parentPublicId));
        }
        return CallAsync(() => api.ExecuteAsync());
    }

    public Task<ServiceActionResult<OrganizationItem>> UpdateAsync(string publicId, OrganizationItem item) =>
        CallAsync(() => Api<OrganizationItem>($"api/organizations/{Segment(publicId)}").Put(item).ExecuteAsync());

    public Task<ServiceActionResult<OrganizationItem>> ToggleActiveAsync(string publicId) =>
        CallAsync(() => Api<OrganizationItem>($"api/organizations/{Segment(publicId)}/toggle-active").Post<object?>(null).ExecuteAsync());

    public Task<ServiceActionResult<OrganizationItem>> MoveAsync(string publicId, string newParentPublicId) =>
        CallAsync(() => Api<OrganizationItem>($"api/organizations/{Segment(publicId)}/move")
            .Post<object?>(null).Parameter("parentId", Segment(newParentPublicId)).ExecuteAsync());
}
