using System.Text.Json;
using DevInstance.DevCoreApp.Server.Admin.Services.Background.Requests;
using DevInstance.DevCoreApp.Server.Admin.Services.BackgroundTasks;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Shared.Model.Common;
using DevInstance.LogScope;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Background;

public class BackgroundWorker : BackgroundService, IBackgroundWorker
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBackgroundTaskWorker _taskWorker;
    private readonly IScopeLog _log;

    public DateTime? LastHeartbeat => _taskWorker.LastHeartbeat;
    public int QueueLength => _taskWorker.QueueLength;

    public BackgroundWorker(
        IServiceScopeFactory scopeFactory,
        IBackgroundTaskWorker taskWorker,
        IScopeManager logManager)
    {
        _scopeFactory = scopeFactory;
        _taskWorker = taskWorker;
        _log = logManager.CreateLogger(this);
    }

    public async Task SubmitAsync(BackgroundRequestItem item)
    {
        using var scope = _scopeFactory.CreateScope();
        var operationContext = scope.ServiceProvider.GetRequiredService<BackgroundOperationContext>();
        operationContext.Reset();

        var repository = scope.ServiceProvider.GetRequiredService<IQueryRepository>();
        var query = repository.GetBackgroundTaskQuery(null!);
        var task = query.CreateNew();

        task.TaskType = MapRequestType(item.RequestType);
        task.Payload = SerializePayload(item.Content);
        task.Status = BackgroundTaskStatus.Queued;
        task.MaxRetries = 3;
        task.ScheduledAt = DateTime.UtcNow;
        task.ResultReference = ExtractResultReference(item);

        await query.AddAsync(task);

        _taskWorker.Enqueue(task.Id);
    }

    public void Submit(BackgroundRequestItem item)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await SubmitAsync(item);
            }
            catch (Exception ex)
            {
                _log.E($"Failed to persist background task: {ex.Message}");
            }
        });
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _taskWorker.ExecuteAsync(stoppingToken);
    }

    private static string MapRequestType(BackgroundRequestType requestType) => requestType switch
    {
        BackgroundRequestType.SendEmail => BackgroundTaskTypes.SendEmail,
        _ => requestType.ToString()
    };

    private static string SerializePayload(object content)
    {
        return JsonSerializer.Serialize(content, content.GetType());
    }

    private static string? ExtractResultReference(BackgroundRequestItem item)
    {
        if (item.RequestType == BackgroundRequestType.SendEmail && item.Content is EmailRequest emailRequest)
        {
            return !string.IsNullOrEmpty(emailRequest.EmailLogId)
                ? $"EmailLog:{emailRequest.EmailLogId}"
                : null;
        }
        return null;
    }
}
