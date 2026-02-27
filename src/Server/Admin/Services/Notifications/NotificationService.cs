using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Admin.Services.Background;
using DevInstance.DevCoreApp.Server.Admin.Services.Background.Requests;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.EmailProcessor;
using DevInstance.DevCoreApp.Shared.Model.Common;
using DevInstance.DevCoreApp.Shared.Model.Notifications;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;
using DevInstance.WebServiceToolkit.Database.Queries.Extensions;
using DevInstance.WebServiceToolkit.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Notifications;

[BlazorService]
public class NotificationService : BaseService, INotificationService
{
    private readonly IScopeLog log;
    private readonly UserManager<ApplicationUser> UserManager;
    private readonly IBackgroundWorker BackgroundWorker;
    private readonly INotificationHubService NotificationHub;

    public NotificationService(IScopeManager logManager,
                               ITimeProvider timeProvider,
                               IQueryRepository query,
                               IAuthorizationContext authorizationContext,
                               UserManager<ApplicationUser> userManager,
                               IBackgroundWorker backgroundWorker,
                               INotificationHubService notificationHub)
        : base(logManager, timeProvider, query, authorizationContext)
    {
        log = logManager.CreateLogger(this);
        UserManager = userManager;
        BackgroundWorker = backgroundWorker;
        NotificationHub = notificationHub;
    }

    public async Task<ServiceActionResult<NotificationItem>> SendAsync(
        Guid userProfileId, NotificationType type, string title, string message,
        string? linkUrl = null, string? groupKey = null, string? category = null)
    {
        using var l = log.TraceScope();

        var userProfile = await Repository.GetUserProfilesQuery(null)
            .Select()
            .FirstOrDefaultAsync(u => u.Id == userProfileId);

        if (userProfile == null)
        {
            throw new RecordNotFoundException("User profile not found.");
        }

        bool inAppEnabled = true;
        bool emailEnabled = false;

        if (!string.IsNullOrEmpty(category))
        {
            var preference = await Repository.GetUserNotificationPreferenceQuery(null)
                .ByUserProfileId(userProfileId)
                .ByCategory(category)
                .Select()
                .FirstOrDefaultAsync();

            if (preference != null)
            {
                inAppEnabled = preference.InAppEnabled;
                emailEnabled = preference.EmailEnabled;
            }
            else
            {
                // No preference record — defaults: in-app on, email on
                emailEnabled = true;
            }
        }

        NotificationItem notificationItem = null;

        if (inAppEnabled)
        {
            var organizationId = await GetUserPrimaryOrganizationIdAsync(userProfile);

            var notificationQuery = Repository.GetNotificationQuery(null);
            var notification = notificationQuery.CreateNew();
            notification.UserProfileId = userProfileId;
            notification.OrganizationId = organizationId;
            notification.Type = type;
            notification.Title = title;
            notification.Message = message;
            notification.LinkUrl = linkUrl;
            notification.GroupKey = groupKey;
            notification.IsRead = false;

            await notificationQuery.AddAsync(notification);

            notificationItem = notification.ToView();

            await NotificationHub.SendNotificationAsync(
                userProfile.ApplicationUserId, notificationItem);

            var unreadCount = await Repository.GetNotificationQuery(null)
                .ByUserProfileId(userProfileId)
                .ByIsRead(false)
                .Select()
                .CountAsync();

            await NotificationHub.SendUnreadCountAsync(
                userProfile.ApplicationUserId, unreadCount);
        }

        if (emailEnabled && !string.IsNullOrEmpty(userProfile.Email))
        {
            await QueueNotificationEmailAsync(userProfile, title, message);
        }

        l.I($"Notification sent to user {userProfileId}: {title}");
        return ServiceActionResult<NotificationItem>.OK(notificationItem);
    }

    public async Task<ServiceActionResult<int>> SendToRoleAsync(
        string roleName, NotificationType type, string title, string message,
        string? linkUrl = null, string? groupKey = null, string? category = null)
    {
        using var l = log.TraceScope();

        var usersInRole = await UserManager.GetUsersInRoleAsync(roleName);
        int sentCount = 0;

        foreach (var appUser in usersInRole)
        {
            var userProfile = await Repository.GetUserProfilesQuery(null)
                .ByApplicationUserId(appUser.Id)
                .Select()
                .FirstOrDefaultAsync();

            if (userProfile != null)
            {
                await SendAsync(userProfile.Id, type, title, message, linkUrl, groupKey, category);
                sentCount++;
            }
        }

        l.I($"Notification sent to {sentCount} user(s) in role '{roleName}': {title}");
        return ServiceActionResult<int>.OK(sentCount);
    }

    public async Task<ServiceActionResult<int>> SendToOrganizationAsync(
        Guid organizationId, NotificationType type, string title, string message,
        string? linkUrl = null, string? groupKey = null, string? category = null)
    {
        using var l = log.TraceScope();

        var userProfiles = await Repository.GetUserProfilesQuery(null)
            .ByOrganizationId(organizationId)
            .Select()
            .ToListAsync();

        int sentCount = 0;

        foreach (var userProfile in userProfiles)
        {
            await SendAsync(userProfile.Id, type, title, message, linkUrl, groupKey, category);
            sentCount++;
        }

        l.I($"Notification sent to {sentCount} user(s) in organization {organizationId}: {title}");
        return ServiceActionResult<int>.OK(sentCount);
    }

    public async Task<ServiceActionResult<NotificationItem>> MarkAsReadAsync(string notificationId)
    {
        using var l = log.TraceScope();

        var query = Repository.GetNotificationQuery(AuthorizationContext.CurrentProfile);
        var notification = await query
            .ByPublicId(notificationId)
            .Select()
            .FirstOrDefaultAsync();

        if (notification == null)
        {
            throw new RecordNotFoundException("Notification not found.");
        }

        notification.IsRead = true;
        notification.ReadAt = TimeProvider.CurrentTime;
        await query.UpdateAsync(notification);

        var userProfile = await Repository.GetUserProfilesQuery(null)
            .Select()
            .FirstOrDefaultAsync(u => u.Id == notification.UserProfileId);

        if (userProfile != null)
        {
            var unreadCount = await Repository.GetNotificationQuery(null)
                .ByUserProfileId(notification.UserProfileId)
                .ByIsRead(false)
                .Select()
                .CountAsync();

            await NotificationHub.SendUnreadCountAsync(
                userProfile.ApplicationUserId, unreadCount);
        }

        l.I($"Notification {notificationId} marked as read.");
        return ServiceActionResult<NotificationItem>.OK(notification.ToView());
    }

    public async Task<ServiceActionResult<int>> MarkAllReadAsync(Guid userProfileId)
    {
        using var l = log.TraceScope();

        var unreadNotifications = await Repository.GetNotificationQuery(null)
            .ByUserProfileId(userProfileId)
            .ByIsRead(false)
            .Select()
            .ToListAsync();

        var now = TimeProvider.CurrentTime;
        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
            var updateQuery = Repository.GetNotificationQuery(null);
            await updateQuery.UpdateAsync(notification);
        }

        var userProfile = await Repository.GetUserProfilesQuery(null)
            .Select()
            .FirstOrDefaultAsync(u => u.Id == userProfileId);

        if (userProfile != null)
        {
            await NotificationHub.SendUnreadCountAsync(
                userProfile.ApplicationUserId, 0);
        }

        l.I($"Marked {unreadNotifications.Count} notification(s) as read for user {userProfileId}.");
        return ServiceActionResult<int>.OK(unreadNotifications.Count);
    }

    public async Task<ServiceActionResult<int>> GetUnreadCountAsync(Guid userProfileId)
    {
        using var l = log.TraceScope();

        var count = await Repository.GetNotificationQuery(null)
            .ByUserProfileId(userProfileId)
            .ByIsRead(false)
            .Select()
            .CountAsync();

        return ServiceActionResult<int>.OK(count);
    }

    public async Task<ServiceActionResult<ModelList<NotificationItem>>> GetNotificationsAsync(
        Guid userProfileId, int? page = null, int? pageSize = null)
    {
        using var l = log.TraceScope();

        var query = Repository.GetNotificationQuery(null)
            .ByUserProfileId(userProfileId)
            .SortBy("createdate", false);

        var totalCount = await query.Clone().Select().CountAsync();
        var notifications = await query.Paginate(pageSize, page).Select().ToListAsync();

        var items = notifications.Select(n => n.ToView()).ToArray();
        var modelList = ModelListResult.CreateList(items, totalCount, pageSize, page);

        return ServiceActionResult<ModelList<NotificationItem>>.OK(modelList);
    }

    private async Task<Guid> GetUserPrimaryOrganizationIdAsync(UserProfile userProfile)
    {
        var appUser = await UserManager.FindByIdAsync(userProfile.ApplicationUserId.ToString());
        if (appUser?.PrimaryOrganizationId != null)
        {
            return appUser.PrimaryOrganizationId.Value;
        }
        return Guid.Empty;
    }

    private async Task QueueNotificationEmailAsync(UserProfile userProfile, string subject, string body)
    {
        var emailRequest = new EmailRequest
        {
            From = new EmailAddress { Address = "notifications@system.local", Name = "Notifications" },
            To = new List<EmailAddress>
            {
                new EmailAddress
                {
                    Address = userProfile.Email,
                    Name = $"{userProfile.FirstName} {userProfile.LastName}".Trim()
                }
            },
            Subject = subject,
            IsHtml = false,
            Content = body,
            TemplateName = "Notification"
        };

        await BackgroundWorker.SubmitAsync(new BackgroundRequestItem
        {
            RequestType = BackgroundRequestType.SendEmail,
            Content = emailRequest
        });
    }
}
