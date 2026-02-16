using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Admin.Services.Background.Requests;
using DevInstance.DevCoreApp.Server.Admin.Services.Email;
using DevInstance.LogScope;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Background;

public class BackgroundWorker : BackgroundService, IBackgroundWorker
{
    protected readonly ConcurrentQueue<BackgroundRequestItem> theQueue = new();
    protected readonly IServiceScopeFactory factory;
    protected readonly IConfiguration config;
    private readonly IScopeLog log;

    public BackgroundWorker(IServiceScopeFactory factory, IConfiguration config, IScopeManager manager)
    {
        this.factory = factory;
        this.config = config;
        log = manager.CreateLogger(this);
    }

    public void Submit(BackgroundRequestItem item)
    {
        theQueue.Enqueue(item);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var scope = factory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var repository = scope.ServiceProvider.GetRequiredService<IQueryRepository>();

            while (!stoppingToken.IsCancellationRequested)
            {
                var utcNow = DateTime.UtcNow;
                if (theQueue.TryDequeue(out var request))
                {
                    try
                    {
                        switch (request.RequestType)
                        {
                            case BackgroundRequestType.SendEmail:
                            {
                                var emailRequest = (EmailRequest)request.Content;
                                await ProcessEmailRequestAsync(scope, repository, emailRequest);
                            }
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        log.E($"Background worker error: {ex.Message}");
                    }
                }
                else
                {
#if DEBUG
                    await Task.Delay(1 * 1000, stoppingToken); // avoid tight loop
#else
                    await Task.Delay(10 * 1000, stoppingToken);
#endif
                }
            }
        }
    }

    private async Task ProcessEmailRequestAsync(IServiceScope scope, IQueryRepository repository, EmailRequest emailRequest)
    {
        using var l = log.TraceScope();

        var query = repository.GetEmailLogQuery(null!);
        EmailLog emailLog;

        if (!string.IsNullOrEmpty(emailRequest.EmailLogId))
        {
            emailLog = await query.ByPublicId(emailRequest.EmailLogId).Select().FirstOrDefaultAsync();
            if (emailLog == null)
            {
                l.E($"Email log entry {emailRequest.EmailLogId} not found for resend.");
                return;
            }
        }
        else
        {
            emailLog = query.CreateNew();
            emailLog.ToRecord(emailRequest, emailRequest.TemplateName, DateTime.UtcNow);
            await query.AddAsync(emailLog);
            l.I($"Created email log entry {emailLog.PublicId} with Batched status.");
        }

        try
        {
            var emailSenderService = scope.ServiceProvider.GetRequiredService<IEmailSenderService>();
            await emailSenderService.SendAsync(emailRequest);

            emailLog.Status = EmailLogStatus.Sent;
            emailLog.SentDate = DateTime.UtcNow;
            emailLog.ErrorMessage = null;
            var updateQuery = repository.GetEmailLogQuery(null!);
            await updateQuery.UpdateAsync(emailLog);
            l.I($"Email log entry {emailLog.PublicId} marked as Sent.");
        }
        catch (Exception ex)
        {
            emailLog.Status = EmailLogStatus.Failed;
            emailLog.ErrorMessage = ex.Message;
            var updateQuery = repository.GetEmailLogQuery(null!);
            await updateQuery.UpdateAsync(emailLog);
            l.E($"Email log entry {emailLog.PublicId} marked as Failed: {ex.Message}");
        }
    }
}
