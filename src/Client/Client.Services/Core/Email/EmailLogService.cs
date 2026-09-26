using DevInstance.BlazorToolkit.Http;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Client.Services.Core.Time;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.LogScope;
using DevInstance.DevCoreApp.Shared.Model.Core;

namespace DevInstance.DevCoreApp.Client.Services.Core.Email;

[BlazorService]
public class EmailLogService : ApiServiceBase, IEmailLogService
{
    private readonly ILocalTimeService time;

    public EmailLogService(IHttpApiContextFactory apiFactory, ILocalTimeService time, IScopeManager logManager)
        : base(apiFactory, logManager)
    {
        this.time = time;
    }

    public Task<ServiceActionResult<PagedList<EmailLogItem>>> GetAllAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null,
        string? search = null, int? status = null, string? templateName = null,
        DateTime? startDate = null, DateTime? endDate = null) =>
        CallAsync(() => Api<EmailLogItem>("api/email-logs").Get()
            .Query(new EmailLogQuery
            {
                Top = top ?? 20, Page = page ?? 0, SortBy = SortBy(sortField, isAsc)!, Search = search!,
                Status = status, TemplateName = templateName!,
                StartDate = time.ToUtc(startDate), EndDate = time.ToUtc(endDate)
            })
            .ExecuteAsync<PagedList<EmailLogItem>>());

    public Task<ServiceActionResult<EmailLogItem>> GetAsync(string id) =>
        CallAsync(() => Api<EmailLogItem>($"api/email-logs/{Segment(id)}").Get().ExecuteAsync());

    public Task<ServiceActionResult<EmailLogItem>> DeleteAsync(string id) =>
        CallAsync(() => Api<EmailLogItem>($"api/email-logs/{Segment(id)}").Delete().ExecuteAsync());

    public Task<ServiceActionResult<bool>> DeleteMultipleAsync(List<string> publicIds) =>
        CallAsync(() => Api<bool>("api/email-logs/bulk-delete").Post<List<string>>(publicIds).ExecuteAsync());

    public Task<ServiceActionResult<bool>> ResendAsync(string publicId) =>
        CallAsync(() => Api<bool>($"api/email-logs/{Segment(publicId)}/resend").Post<object?>(null).ExecuteAsync());

    public Task<ServiceActionResult<int>> ResendAllFailedAsync(
        int? status = null, DateTime? startDate = null, DateTime? endDate = null, string? search = null) =>
        CallAsync(() => Api<int>("api/email-logs/resend-failed")
            .Post<object?>(null)
            .Query(new EmailLogQuery
            {
                Status = status, Search = search!,
                StartDate = time.ToUtc(startDate), EndDate = time.ToUtc(endDate)
            })
            .ExecuteAsync());
}
