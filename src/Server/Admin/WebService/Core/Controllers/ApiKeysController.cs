using DevInstance.DevCoreApp.Server.Admin.Services.Core.ApiKeys;
using DevInstance.DevCoreApp.Shared.Model.Core.ApiKeys;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Core.Controllers;

[Route("api/api-keys")]
[Authorize]
public class ApiKeysController : ApiControllerBase
{
    private readonly IApiKeyAdminService _service;

    public ApiKeysController(IApiKeyAdminService service) => _service = service;

    [HttpGet]
    [Authorize(Policy = PermissionDefinitions.Admin.ApiKeys.View)]
    public Task<ActionResult<PagedList<ApiKeyItem>>> GetListAsync([FromQuery] ListQuery query)
        => HandleServiceAsync(() => _service.GetKeysAsync(query.Top, query.Page, query.SortBy, query.Search));

    /// <summary>The plain-text key is only in this response; it is never retrievable again.</summary>
    [HttpPost]
    [Authorize(Policy = PermissionDefinitions.Admin.ApiKeys.Create)]
    public Task<ActionResult<ApiKeyCreateResult>> CreateAsync([FromBody] ApiKeyItem item)
        => HandleServiceAsync(() => _service.CreateKeyAsync(item));

    [HttpPost("{id}/revoke")]
    [Authorize(Policy = PermissionDefinitions.Admin.ApiKeys.Revoke)]
    public Task<ActionResult<bool>> RevokeAsync(string id)
        => HandleServiceAsync(() => _service.RevokeKeyAsync(id));
}
