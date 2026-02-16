using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.WebServiceToolkit.Common.Model;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Email;

public interface IEmailLogService : ICRUDService<EmailLogItem>
{
    Task<ServiceActionResult<ModelList<EmailLogItem>>> GetAllAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null,
        string? search = null, int? status = null,
        DateTime? startDate = null, DateTime? endDate = null);

    Task<ServiceActionResult<bool>> DeleteMultipleAsync(List<string> publicIds);

    Task<ServiceActionResult<bool>> ResendAsync(string publicId);

    Task<ServiceActionResult<int>> ResendAllFailedAsync(
        int? status = null, DateTime? startDate = null, DateTime? endDate = null, string? search = null);
}
