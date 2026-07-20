using System;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data;

// IAsyncDisposable so a repository created per-operation via IQueryRepositoryFactory
// disposes the short-lived DbContext it owns. The DI-scoped registration owns no context
// and its DisposeAsync is a no-op (see CoreQueryRepository.ownsContext).
public interface IQueryRepository : IAsyncDisposable
{
    IUserProfilesQuery GetUserProfilesQuery(UserProfile currentProfile);
    IGridProfilesQuery GetGridProfilesQuery(UserProfile currentProfile);
    IEmailLogQuery GetEmailLogQuery(UserProfile currentProfile);
    IOrganizationsQuery GetOrganizationsQuery(UserProfile currentProfile);
    IUserLoginHistoryQuery GetUserLoginHistoryQuery(UserProfile currentProfile);
    ISettingsQuery GetSettingsQuery(UserProfile currentProfile);
    IBackgroundTaskQuery GetBackgroundTaskQuery(UserProfile currentProfile);
    IBackgroundTaskLogQuery GetBackgroundTaskLogQuery(UserProfile currentProfile);
    INotificationTemplateQuery GetNotificationTemplateQuery(UserProfile currentProfile);
    INotificationQuery GetNotificationQuery(UserProfile currentProfile);
    IUserNotificationPreferenceQuery GetUserNotificationPreferenceQuery(UserProfile currentProfile);
    IFileRecordQuery GetFileRecordQuery(UserProfile currentProfile);
    IAuditLogQuery GetAuditLogQuery(UserProfile currentProfile);
    IImportSessionQuery GetImportSessionQuery(UserProfile currentProfile);
    IFeatureFlagQuery GetFeatureFlagQuery(UserProfile currentProfile);
    IApiKeyQuery GetApiKeyQuery(UserProfile currentProfile);
    IWebhookSubscriptionQuery GetWebhookSubscriptionQuery(UserProfile currentProfile);
    IWebhookDeliveryQuery GetWebhookDeliveryQuery(UserProfile currentProfile);
    IRefreshTokenQuery GetRefreshTokenQuery(UserProfile currentProfile);
    IUserOrganizationQuery GetUserOrganizationQuery(UserProfile currentProfile);
    IPermissionQuery GetPermissionQuery(UserProfile currentProfile);
    IUserPermissionOverrideQuery GetUserPermissionOverrideQuery(UserProfile currentProfile);
    IRolePermissionQuery GetRolePermissionQuery(UserProfile currentProfile);
}
