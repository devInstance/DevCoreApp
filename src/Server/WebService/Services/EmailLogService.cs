using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.EmailProcessor;
using DevInstance.DevCoreApp.Server.WebService.Authentication;
using DevInstance.DevCoreApp.Server.WebService.Background;
using DevInstance.DevCoreApp.Server.WebService.Background.Requests;
using DevInstance.DevCoreApp.Server.WebService.Tools;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;
using DevInstance.WebServiceToolkit.Database.Queries.Extensions;
using Microsoft.EntityFrameworkCore;

namespace DevInstance.DevCoreApp.Server.WebService.Services;

[AppService]
public class EmailLogService : BaseService
{
    private IBackgroundWorker BackgroundWorker { get; }

    private IScopeLog log;

    public EmailLogService(IScopeManager logManager,
                           ITimeProvider timeProvider,
                           IQueryRepository query,
                           IAuthorizationContext authorizationContext,
                           IBackgroundWorker backgroundWorker)
        : base(logManager, timeProvider, query, authorizationContext)
    {
        log = logManager.CreateLogger(this);
        BackgroundWorker = backgroundWorker;
    }

    public async Task<ServiceActionResult<ModelList<EmailLogItem>>> GetAllAsync(
        int? top, int? page, string? sortField = null, bool? isAsc = null,
        string? search = null, int? status = null,
        DateTime? startDate = null, DateTime? endDate = null)
    {
        using var l = log.TraceScope();

        var query = Repository.GetEmailLogQuery(AuthorizationContext.CurrentProfile);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Search(search);
        }

        if (status.HasValue && Enum.IsDefined(typeof(EmailLogStatus), status.Value))
        {
            query = query.ByStatus((EmailLogStatus)status.Value);
        }

        if (startDate.HasValue || endDate.HasValue)
        {
            query = query.ByDateRange(startDate, endDate);
        }

        if (!string.IsNullOrEmpty(sortField))
        {
            query = query.SortBy(sortField, isAsc ?? true);
        }

        var totalCount = await query.Clone().Select().CountAsync();
        var emailLogs = await query.Paginate(top, page).Select().ToListAsync();

        var items = emailLogs.Select(e => e.ToView()).ToArray();

        var modelList = ModelListResult.CreateList(items, totalCount, top, page, sortField, isAsc, search);
        return ServiceActionResult<ModelList<EmailLogItem>>.OK(modelList);
    }

    public async Task<ServiceActionResult<EmailLogItem>> GetByIdAsync(string publicId)
    {
        using var l = log.TraceScope();

        var emailLog = await Repository.GetEmailLogQuery(AuthorizationContext.CurrentProfile)
            .ByPublicId(publicId)
            .Select()
            .FirstOrDefaultAsync();

        if (emailLog == null)
        {
            throw new InvalidOperationException("Email log entry not found.");
        }

        return ServiceActionResult<EmailLogItem>.OK(emailLog.ToView());
    }

    public async Task<ServiceActionResult<bool>> DeleteAsync(string publicId)
    {
        using var l = log.TraceScope();

        var query = Repository.GetEmailLogQuery(AuthorizationContext.CurrentProfile);
        var emailLog = await query.ByPublicId(publicId).Select().FirstOrDefaultAsync();

        if (emailLog == null)
        {
            throw new InvalidOperationException("Email log entry not found.");
        }

        await query.RemoveAsync(emailLog);
        l.I($"Email log entry {publicId} deleted.");

        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<bool>> DeleteMultipleAsync(List<string> publicIds)
    {
        using var l = log.TraceScope();

        foreach (var publicId in publicIds)
        {
            var query = Repository.GetEmailLogQuery(AuthorizationContext.CurrentProfile);
            var emailLog = await query.ByPublicId(publicId).Select().FirstOrDefaultAsync();

            if (emailLog != null)
            {
                await query.RemoveAsync(emailLog);
            }
        }

        l.I($"Deleted {publicIds.Count} email log entries.");
        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<bool>> ResendAsync(string publicId)
    {
        using var l = log.TraceScope();

        var query = Repository.GetEmailLogQuery(AuthorizationContext.CurrentProfile);
        var emailLog = await query.ByPublicId(publicId).Select().FirstOrDefaultAsync();

        if (emailLog == null)
        {
            throw new InvalidOperationException("Email log entry not found.");
        }

        emailLog.Status = EmailLogStatus.Batched;
        emailLog.ErrorMessage = null;
        emailLog.SentDate = null;
        emailLog.ScheduledDate = TimeProvider.CurrentTime;
        await query.UpdateAsync(emailLog);

        var emailRequest = new EmailRequest
        {
            From = new EmailAddress { Address = emailLog.FromAddress, Name = emailLog.FromName },
            To = new List<EmailAddress>
            {
                new EmailAddress { Address = emailLog.ToAddress, Name = emailLog.ToName }
            },
            Subject = emailLog.Subject,
            IsHtml = emailLog.IsHtml,
            Content = emailLog.Content,
            EmailLogId = emailLog.PublicId,
            TemplateName = emailLog.TemplateName
        };

        BackgroundWorker.Submit(new BackgroundRequestItem
        {
            RequestType = BackgroundRequestType.SendEmail,
            Content = emailRequest
        });

        l.I($"Email log entry {publicId} re-queued for sending.");
        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<int>> ResendAllFailedAsync(
        int? status = null, DateTime? startDate = null, DateTime? endDate = null, string? search = null)
    {
        using var l = log.TraceScope();

        var query = Repository.GetEmailLogQuery(AuthorizationContext.CurrentProfile)
            .ByStatus(EmailLogStatus.Failed);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Search(search);
        }

        if (startDate.HasValue || endDate.HasValue)
        {
            query = query.ByDateRange(startDate, endDate);
        }

        var failedEmails = await query.Select().ToListAsync();
        var count = 0;

        foreach (var emailLog in failedEmails)
        {
            emailLog.Status = EmailLogStatus.Batched;
            emailLog.ErrorMessage = null;
            emailLog.SentDate = null;
            emailLog.ScheduledDate = TimeProvider.CurrentTime;

            var updateQuery = Repository.GetEmailLogQuery(AuthorizationContext.CurrentProfile);
            await updateQuery.UpdateAsync(emailLog);

            var emailRequest = new EmailRequest
            {
                From = new EmailAddress { Address = emailLog.FromAddress, Name = emailLog.FromName },
                To = new List<EmailAddress>
                {
                    new EmailAddress { Address = emailLog.ToAddress, Name = emailLog.ToName }
                },
                Subject = emailLog.Subject,
                IsHtml = emailLog.IsHtml,
                Content = emailLog.Content,
                EmailLogId = emailLog.PublicId,
                TemplateName = emailLog.TemplateName
            };

            BackgroundWorker.Submit(new BackgroundRequestItem
            {
                RequestType = BackgroundRequestType.SendEmail,
                Content = emailRequest
            });

            count++;
        }

        l.I($"Re-queued {count} failed email(s) for sending.");
        return ServiceActionResult<int>.OK(count);
    }
}
