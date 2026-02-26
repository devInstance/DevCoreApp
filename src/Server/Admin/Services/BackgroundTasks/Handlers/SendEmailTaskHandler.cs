using System.Text.Json;
using DevInstance.DevCoreApp.Server.Admin.Services.Background.Requests;
using DevInstance.DevCoreApp.Server.Admin.Services.Email;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.LogScope;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevInstance.DevCoreApp.Server.Admin.Services.BackgroundTasks.Handlers;

public class SendEmailTaskHandler : IBackgroundTaskHandler
{
    public string TaskType => BackgroundTaskTypes.SendEmail;

    private readonly IScopeLog log;

    public SendEmailTaskHandler(IScopeManager logManager)
    {
        log = logManager.CreateLogger(this);
    }

    public async Task HandleAsync(string payload, IServiceProvider scopedProvider, CancellationToken cancellationToken)
    {
        using var l = log.TraceScope();

        var emailRequest = JsonSerializer.Deserialize<EmailRequest>(payload)
            ?? throw new InvalidOperationException("Failed to deserialize email request payload.");

        var repository = scopedProvider.GetRequiredService<IQueryRepository>();
        var query = repository.GetEmailLogQuery(null!);
        EmailLog emailLog;

        if (!string.IsNullOrEmpty(emailRequest.EmailLogId))
        {
            emailLog = await query.ByPublicId(emailRequest.EmailLogId).Select().FirstOrDefaultAsync(cancellationToken);
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
            var emailSenderService = scopedProvider.GetRequiredService<IEmailSenderService>();
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
            throw;
        }
    }
}
