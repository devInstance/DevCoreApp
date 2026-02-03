using System;
using System.Linq;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.EmailProcessor;
using DevInstance.DevCoreApp.Shared.Model;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;

public static class EmailLogDecorators
{
    public static EmailLogItem ToView(this EmailLog emailLog)
    {
        return new EmailLogItem
        {
            Id = emailLog.PublicId,
            FromAddress = emailLog.FromAddress ?? string.Empty,
            FromName = emailLog.FromName ?? string.Empty,
            ToAddress = emailLog.ToAddress ?? string.Empty,
            ToName = emailLog.ToName ?? string.Empty,
            Subject = emailLog.Subject ?? string.Empty,
            Content = emailLog.Content ?? string.Empty,
            IsHtml = emailLog.IsHtml,
            Status = emailLog.Status.ToString(),
            ErrorMessage = emailLog.ErrorMessage,
            ScheduledDate = emailLog.ScheduledDate,
            SentDate = emailLog.SentDate,
            TemplateName = emailLog.TemplateName,
            CreateDate = emailLog.CreateDate,
            UpdateDate = emailLog.UpdateDate
        };
    }

    public static EmailLog ToRecord(this EmailLog emailLog, EmailLogItem item)
    {
        emailLog.FromAddress = item.FromAddress;
        emailLog.FromName = item.FromName;
        emailLog.ToAddress = item.ToAddress;
        emailLog.ToName = item.ToName;
        emailLog.Subject = item.Subject;
        emailLog.Content = item.Content;
        emailLog.IsHtml = item.IsHtml;
        emailLog.ErrorMessage = item.ErrorMessage;
        emailLog.ScheduledDate = item.ScheduledDate;
        emailLog.SentDate = item.SentDate;
        emailLog.TemplateName = item.TemplateName;

        return emailLog;
    }

    public static EmailLog ToRecord(this EmailLog emailLog, IDevEmailMessage message, string? templateName, DateTime scheduledDate)
    {
        emailLog.FromAddress = message.From.Address;
        emailLog.FromName = message.From.Name;
        emailLog.ToAddress = message.To.FirstOrDefault()?.Address ?? string.Empty;
        emailLog.ToName = message.To.FirstOrDefault()?.Name ?? string.Empty;
        emailLog.Subject = message.Subject;
        emailLog.Content = message.Content;
        emailLog.IsHtml = message.IsHtml;
        emailLog.Status = EmailLogStatus.Batched;
        emailLog.ScheduledDate = scheduledDate;
        emailLog.TemplateName = templateName;

        return emailLog;
    }
}
