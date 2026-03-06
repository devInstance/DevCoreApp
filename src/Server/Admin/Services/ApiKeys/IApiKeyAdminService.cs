using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model.ApiKeys;
using DevInstance.WebServiceToolkit.Common.Model;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.ApiKeys;

public interface IApiKeyAdminService
{
    Task<ServiceActionResult<ModelList<ApiKeyItem>>> GetKeysAsync(
        int top, int page, string[]? sortBy = null, string? search = null);

    Task<ServiceActionResult<ApiKeyCreateResult>> CreateKeyAsync(ApiKeyItem item);

    Task<ServiceActionResult<bool>> RevokeKeyAsync(string id);
}
