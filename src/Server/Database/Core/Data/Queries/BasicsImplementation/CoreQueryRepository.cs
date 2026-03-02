using DevInstance.LogScope;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using NoCrast.Server.Database.Postgres.Data.Queries;

namespace DevInstance.DevCoreApp.Server.Database.Postgres.Data;

public abstract class CoreQueryRepository : IQueryRepository
{
    protected ApplicationDbContext DB { get; }
    public ITimeProvider TimeProvider { get; }

    private IScopeLog log;
    private IScopeManager LogManager;

    public CoreQueryRepository(IScopeManager logManager, ITimeProvider timeProvider, ApplicationDbContext dB)
    {
        LogManager = logManager;
        log = logManager.CreateLogger(this);

        TimeProvider = timeProvider;
        DB = dB;
    }

    public IUserProfilesQuery GetUserProfilesQuery(UserProfile currentProfile)
    {
        return new CoreUserProfilesQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public IGridProfilesQuery GetGridProfilesQuery(UserProfile currentProfile)
    {
        return new CoreGridProfilesQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public IEmailLogQuery GetEmailLogQuery(UserProfile currentProfile)
    {
        return new CoreEmailLogQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public IOrganizationsQuery GetOrganizationsQuery(UserProfile currentProfile)
    {
        return new CoreOrganizationsQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public IUserLoginHistoryQuery GetUserLoginHistoryQuery(UserProfile currentProfile)
    {
        return new CoreUserLoginHistoryQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public ISettingsQuery GetSettingsQuery(UserProfile currentProfile)
    {
        return new CoreSettingsQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public IBackgroundTaskQuery GetBackgroundTaskQuery(UserProfile currentProfile)
    {
        return new CoreBackgroundTaskQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public IBackgroundTaskLogQuery GetBackgroundTaskLogQuery(UserProfile currentProfile)
    {
        return new CoreBackgroundTaskLogQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public INotificationTemplateQuery GetNotificationTemplateQuery(UserProfile currentProfile)
    {
        return new CoreNotificationTemplateQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public INotificationQuery GetNotificationQuery(UserProfile currentProfile)
    {
        return new CoreNotificationQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public IUserNotificationPreferenceQuery GetUserNotificationPreferenceQuery(UserProfile currentProfile)
    {
        return new CoreUserNotificationPreferenceQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public IFileRecordQuery GetFileRecordQuery(UserProfile currentProfile)
    {
        return new CoreFileRecordQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public IAuditLogQuery GetAuditLogQuery(UserProfile currentProfile)
    {
        return new CoreAuditLogQuery(LogManager, TimeProvider, DB, currentProfile);
    }
}
