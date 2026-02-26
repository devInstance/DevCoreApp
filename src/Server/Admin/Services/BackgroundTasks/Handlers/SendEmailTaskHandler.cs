using System.Text.Json;
using DevInstance.DevCoreApp.Server.Admin.Services.Background.Requests;
using DevInstance.DevCoreApp.Server.Admin.Services.Email;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
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

        if (string.IsNullOrEmpty(emailRequest.EmailLogId))
        {
            throw new InvalidOperationException("EmailLogId is required. EmailLog must be created before submitting the background task.");
        }

        var repository = scopedProvider.GetRequiredService<IQueryRepository>();
        var query = repository.GetEmailLogQuery(null!);

        var emailLog = await query.ByPublicId(emailRequest.EmailLogId).Select().FirstOrDefaultAsync(cancellationToken);
        if (emailLog == null)
        {
            l.E($"Email log entry {emailRequest.EmailLogId} not found.");
            return;
        }

        try
        {
            var emailSenderService = scopedProvider.GetRequiredService<IEmailSenderService>();
            var result = await emailSenderService.SendAsync(emailRequest);

            emailLog.Status = result.Success ? EmailLogStatus.Sent : EmailLogStatus.Failed;
            emailLog.SentDate = result.Success ? DateTime.UtcNow : null;
            emailLog.ProviderMessageId = result.ProviderId;
            emailLog.ErrorMessage = result.Success ? null : result.ErrorMessage;

            var updateQuery = repository.GetEmailLogQuery(null!);
            await updateQuery.UpdateAsync(emailLog);

            if (result.Success)
            {
                l.I($"Email log entry {emailLog.PublicId} marked as Sent (provider: {result.ProviderId}).");
            }
            else
            {
                l.E($"Email log entry {emailLog.PublicId} marked as Failed: {result.ErrorMessage}");
                throw new InvalidOperationException($"Email provider returned failure: {result.ErrorMessage}");
            }
        }
        catch (Exception ex) when (ex is not InvalidOperationException { Message: var m } || !m.StartsWith("Email provider returned failure:"))
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
