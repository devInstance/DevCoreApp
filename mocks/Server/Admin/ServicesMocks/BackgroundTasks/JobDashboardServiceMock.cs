using Bogus;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.BackgroundTasks;
using DevInstance.DevCoreApp.Shared.Model.BackgroundTasks;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Mocks.BackgroundTasks;

[BlazorServiceMock]
public class JobDashboardServiceMock : IJobDashboardService
{
    const int TotalCount = 30;
    List<BackgroundTaskItem> modelList;
    Dictionary<string, List<BackgroundTaskLogItem>> logsByJobId;

    private int delay = 500;

    public JobDashboardServiceMock()
    {
        var faker = CreateTaskFaker();
        modelList = faker.Generate(TotalCount);

        logsByJobId = new Dictionary<string, List<BackgroundTaskLogItem>>();
        var logFaker = new Faker();

        foreach (var task in modelList)
        {
            var logCount = task.Status == "Queued" ? 0 : logFaker.Random.Int(1, 3);
            var logs = new List<BackgroundTaskLogItem>();

            for (int i = 1; i <= logCount; i++)
            {
                var logStarted = task.StartedAt ?? task.ScheduledAt;
                var logCompleted = i == logCount ? task.CompletedAt : logStarted.AddSeconds(logFaker.Random.Int(1, 10));
                var logStatus = i == logCount
                    ? (task.Status == "Failed" ? "Failed" : task.Status == "Running" ? "Running" : "Completed")
                    : "Failed";

                logs.Add(new BackgroundTaskLogItem
                {
                    Id = IdGenerator.New(),
                    BackgroundTaskId = task.Id,
                    Attempt = i,
                    Status = logStatus,
                    Message = logStatus == "Completed" ? "Task completed successfully." : null,
                    ErrorMessage = logStatus == "Failed" ? logFaker.PickRandom("Connection timeout", "Invalid payload", "Service unavailable", "Out of memory") : null,
                    StartedAt = logStarted,
                    CompletedAt = logCompleted
                });
            }

            logsByJobId[task.Id] = logs;
        }
    }

    private static Faker<BackgroundTaskItem> CreateTaskFaker()
    {
        return new Faker<BackgroundTaskItem>()
            .RuleFor(t => t.Id, f => IdGenerator.New())
            .RuleFor(t => t.TaskType, f => f.PickRandom("SendEmail", "GenerateReport", "DataSync", "CleanupFiles"))
            .RuleFor(t => t.Status, f => f.PickRandom("Queued", "Running", "Completed", "Failed"))
            .RuleFor(t => t.Priority, f => f.Random.Int(0, 3))
            .RuleFor(t => t.MaxRetries, f => f.Random.Int(1, 5))
            .RuleFor(t => t.RetryCount, (f, t) => t.Status == "Failed" ? f.Random.Int(1, t.MaxRetries) : t.Status == "Completed" ? 0 : 0)
            .RuleFor(t => t.Payload, f => $"{{\"target\":\"{f.Internet.Email()}\"}}")
            .RuleFor(t => t.ResultReference, (f, t) => t.Status == "Completed" ? $"EmailLog:{IdGenerator.New()}" : null)
            .RuleFor(t => t.ErrorMessage, (f, t) => t.Status == "Failed" ? f.PickRandom("SMTP timeout", "Invalid recipient", "Connection refused", "Task handler exception") : null)
            .RuleFor(t => t.ScheduledAt, f => f.Date.Recent(14))
            .RuleFor(t => t.StartedAt, (f, t) => t.Status != "Queued" ? t.ScheduledAt.AddSeconds(f.Random.Int(1, 60)) : null)
            .RuleFor(t => t.CompletedAt, (f, t) => t.Status == "Completed" || t.Status == "Failed" ? t.StartedAt?.AddSeconds(f.Random.Int(1, 120)) : null)
            .RuleFor(t => t.CreatedByName, f => f.Name.FullName())
            .RuleFor(t => t.CreatedById, f => IdGenerator.New())
            .RuleFor(t => t.CreateDate, (f, t) => t.ScheduledAt)
            .RuleFor(t => t.UpdateDate, (f, t) => t.CompletedAt ?? t.StartedAt ?? t.ScheduledAt);
    }

    public async Task<ServiceActionResult<ModelList<BackgroundTaskItem>>> GetAllAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null,
        string? search = null, int? status = null, string? taskType = null,
        DateTime? startDate = null, DateTime? endDate = null)
    {
        var pageVal = page ?? 0;
        var topVal = top ?? 10;

        var filtered = modelList.AsEnumerable();

        if (!string.IsNullOrEmpty(search))
        {
            filtered = filtered.Where(t =>
                t.TaskType.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (t.ResultReference?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.ErrorMessage?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.CreatedByName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (status.HasValue)
        {
            var statusName = ((DevInstance.DevCoreApp.Shared.Model.Common.BackgroundTaskStatus)status.Value).ToString();
            filtered = filtered.Where(t => t.Status == statusName);
        }

        if (!string.IsNullOrEmpty(taskType))
        {
            filtered = filtered.Where(t => t.TaskType == taskType);
        }

        if (startDate.HasValue)
            filtered = filtered.Where(t => t.ScheduledAt >= startDate.Value);
        if (endDate.HasValue)
            filtered = filtered.Where(t => t.ScheduledAt <= endDate.Value);

        var filteredList = filtered.ToList();

        var items = filteredList
            .Skip(pageVal * topVal)
            .Take(topVal)
            .ToArray();

        string[]? sortBy = !string.IsNullOrEmpty(sortField)
            ? new[] { (isAsc == false ? "-" : "") + sortField }
            : null;

        await Task.Delay(delay);

        return ServiceActionResult<ModelList<BackgroundTaskItem>>.OK(
            ModelListResult.CreateList(items, filteredList.Count, topVal, pageVal, sortBy, search));
    }

    public async Task<ServiceActionResult<List<BackgroundTaskLogItem>>> GetJobLogsAsync(string jobPublicId)
    {
        if (!logsByJobId.TryGetValue(jobPublicId, out var logs))
        {
            logs = new List<BackgroundTaskLogItem>();
        }

        await Task.Delay(delay);

        return ServiceActionResult<List<BackgroundTaskLogItem>>.OK(logs);
    }

    public async Task<ServiceActionResult<bool>> CancelJobAsync(string jobPublicId)
    {
        var item = modelList.Find(t => t.Id == jobPublicId);
        if (item == null) throw new InvalidOperationException("Background task not found.");

        item.Status = "Failed";
        item.ErrorMessage = "Cancelled by user";
        item.CompletedAt = DateTime.UtcNow;

        await Task.Delay(delay);

        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<bool>> RetryJobAsync(string jobPublicId)
    {
        var item = modelList.Find(t => t.Id == jobPublicId);
        if (item == null) throw new InvalidOperationException("Background task not found.");

        var newItem = new BackgroundTaskItem
        {
            Id = IdGenerator.New(),
            TaskType = item.TaskType,
            Payload = item.Payload,
            Status = "Queued",
            Priority = item.Priority,
            MaxRetries = item.MaxRetries,
            RetryCount = 0,
            ResultReference = item.ResultReference,
            ScheduledAt = DateTime.UtcNow,
            CreatedByName = item.CreatedByName,
            CreatedById = item.CreatedById,
            CreateDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow
        };

        modelList.Insert(0, newItem);
        logsByJobId[newItem.Id] = new List<BackgroundTaskLogItem>();

        await Task.Delay(delay);

        return ServiceActionResult<bool>.OK(true);
    }
}
