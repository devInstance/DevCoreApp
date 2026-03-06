using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data;

public interface IQueryRepository
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
}
