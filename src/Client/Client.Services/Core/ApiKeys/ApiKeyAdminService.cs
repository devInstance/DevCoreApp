using DevInstance.BlazorToolkit.Http;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Client.Services.Core.Time;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.LogScope;
using DevInstance.DevCoreApp.Shared.Model.Core.ApiKeys;

namespace DevInstance.DevCoreApp.Client.Services.Core.ApiKeys;

[BlazorService]
public class ApiKeyAdminService : ApiServiceBase, IApiKeyAdminService
{
    public ApiKeyAdminService(IHttpApiContextFactory apiFactory, IScopeManager logManager) : base(apiFactory, logManager) { }

    public Task<ServiceActionResult<PagedList<ApiKeyItem>>> GetKeysAsync(int top, int page, string[]? sortBy = null, string? search = null) =>
        CallAsync(() => Api<ApiKeyItem>("api/api-keys").Get()
            .Query(new ListQuery { Top = top, Page = page, SortBy = sortBy!, Search = search! })
            .ExecuteAsync<PagedList<ApiKeyItem>>());

    public Task<ServiceActionResult<ApiKeyCreateResult>> CreateKeyAsync(ApiKeyItem item) =>
        CallAsync(() => Api<ApiKeyCreateResult>("api/api-keys").Post<ApiKeyItem>(item).ExecuteAsync());

    public Task<ServiceActionResult<bool>> RevokeKeyAsync(string id) =>
        CallAsync(() => Api<bool>($"api/api-keys/{Segment(id)}/revoke").Post<object?>(null).ExecuteAsync());
}
