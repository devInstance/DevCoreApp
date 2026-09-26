using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.Core.ApiKeys;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;

namespace DevInstance.DevCoreApp.Client.Services.Core.ApiKeys;

/// <summary>API key administration (<c>api/api-keys</c>). Same shape as the server service.</summary>
public interface IApiKeyAdminService
{
    Task<ServiceActionResult<PagedList<ApiKeyItem>>> GetKeysAsync(int top, int page, string[]? sortBy = null, string? search = null);

    /// <summary>The plain-text key is only in this result; it cannot be retrieved again.</summary>
    Task<ServiceActionResult<ApiKeyCreateResult>> CreateKeyAsync(ApiKeyItem item);

    Task<ServiceActionResult<bool>> RevokeKeyAsync(string id);
}
