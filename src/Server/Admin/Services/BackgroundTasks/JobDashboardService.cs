using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Shared.Model.BackgroundTasks;
using DevInstance.DevCoreApp.Shared.Model.Common;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;
using DevInstance.WebServiceToolkit.Database.Queries.Extensions;
using DevInstance.WebServiceToolkit.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace DevInstance.DevCoreApp.Server.Admin.Services.BackgroundTasks;

[BlazorService]
public class JobDashboardService : BaseService, IJobDashboardService
{
    private IScopeLog log;

    public JobDashboardService(IScopeManager logManager,
                               ITimeProvider timeProvider,
                               IQueryRepository query,
                               IAuthorizationContext authorizationContext)
        : base(logManager, timeProvider, query, authorizationContext)
    {
        log = logManager.CreateLogger(this);
    }

    public async Task<ServiceActionResult<ModelList<BackgroundTaskItem>>> GetAllAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null,
        string? search = null, int? status = null, string? taskType = null,
        DateTime? startDate = null, DateTime? endDate = null)
    {
        using var l = log.TraceScope();

        var query = Repository.GetBackgroundTaskQuery(AuthorizationContext.CurrentProfile);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Search(search);
        }

        if (status.HasValue && Enum.IsDefined(typeof(BackgroundTaskStatus), status.Value))
        {
            query = query.ByStatus((BackgroundTaskStatus)status.Value);
        }

        if (!string.IsNullOrEmpty(taskType))
        {
            query = query.ByTaskType(taskType);
        }

        if (startDate.HasValue || endDate.HasValue)
        {
            query = query.ByDateRange(startDate, endDate);
        }

        query = !string.IsNullOrEmpty(sortField)
            ? query.SortBy(sortField, isAsc ?? true)
            : query.SortBy("scheduledat", false);

        var totalCount = await query.Clone().Select().CountAsync();
        var tasks = await query.Paginate(top, page).Select()
            .Include(bt => bt.CreatedBy)
            .ToListAsync();

        var items = tasks.Select(t => t.ToView()).ToArray();

        string[]? sortBy = !string.IsNullOrEmpty(sortField)
            ? new[] { (isAsc == false ? "-" : "") + sortField }
            : null;

        var modelList = ModelListResult.CreateList(items, totalCount, top, page, sortBy, search);
        return ServiceActionResult<ModelList<BackgroundTaskItem>>.OK(modelList);
    }

    public async Task<ServiceActionResult<List<BackgroundTaskLogItem>>> GetJobLogsAsync(string jobPublicId)
    {
        using var l = log.TraceScope();

        var task = await Repository.GetBackgroundTaskQuery(AuthorizationContext.CurrentProfile)
            .ByPublicId(jobPublicId)
            .Select()
            .FirstOrDefaultAsync();

        if (task == null)
        {
            throw new RecordNotFoundException("Background task not found.");
        }

        var logs = await Repository.GetBackgroundTaskLogQuery(AuthorizationContext.CurrentProfile)
            .ByBackgroundTaskId(task.Id)
            .SortBy("startedat", false)
            .Select()
            .ToListAsync();

        var items = logs.Select(lg => lg.ToView()).ToList();
        return ServiceActionResult<List<BackgroundTaskLogItem>>.OK(items);
    }

    public async Task<ServiceActionResult<bool>> CancelJobAsync(string jobPublicId)
    {
        using var l = log.TraceScope();

        var query = Repository.GetBackgroundTaskQuery(AuthorizationContext.CurrentProfile);
        var task = await query.ByPublicId(jobPublicId).Select().FirstOrDefaultAsync();

        if (task == null)
        {
            throw new RecordNotFoundException("Background task not found.");
        }

        if (task.Status != BackgroundTaskStatus.Queued)
        {
            throw new BadRequestException("Only queued tasks can be cancelled.");
        }

        task.Status = BackgroundTaskStatus.Failed;
        task.ErrorMessage = "Cancelled by user";
        task.CompletedAt = TimeProvider.CurrentTime;
        await query.UpdateAsync(task);

        l.I($"Background task {jobPublicId} cancelled by user.");
        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<bool>> RetryJobAsync(string jobPublicId)
    {
        using var l = log.TraceScope();

        var query = Repository.GetBackgroundTaskQuery(AuthorizationContext.CurrentProfile);
        var task = await query.ByPublicId(jobPublicId)
            .Select()
            .FirstOrDefaultAsync();

        if (task == null)
        {
            throw new RecordNotFoundException("Background task not found.");
        }

        if (task.Status != BackgroundTaskStatus.Failed)
        {
            throw new BadRequestException("Only failed tasks can be retried.");
        }

        var newQuery = Repository.GetBackgroundTaskQuery(AuthorizationContext.CurrentProfile);
        var newTask = newQuery.CreateNew();
        newTask.TaskType = task.TaskType;
        newTask.Payload = task.Payload;
        newTask.Priority = task.Priority;
        newTask.MaxRetries = task.MaxRetries;
        newTask.ResultReference = task.ResultReference;
        newTask.OrganizationId = task.OrganizationId;
        newTask.CreatedById = task.CreatedById;
        newTask.Status = BackgroundTaskStatus.Queued;
        newTask.ScheduledAt = TimeProvider.CurrentTime;
        await newQuery.AddAsync(newTask);

        l.I($"Background task {jobPublicId} retried. New task: {newTask.PublicId}.");
        return ServiceActionResult<bool>.OK(true);
    }
}
