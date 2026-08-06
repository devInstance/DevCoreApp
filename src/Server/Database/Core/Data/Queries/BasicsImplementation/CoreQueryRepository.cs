using System;
using System.Threading.Tasks;
using DevInstance.LogScope;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

namespace DevInstance.DevCoreApp.Server.Database.Postgres.Data;

// Implements IDisposable in addition to IQueryRepository's IAsyncDisposable so the DI-scoped
// registration can be torn down by a SYNCHRONOUSLY-disposed scope (e.g. `using var scope =
// provider.CreateScope()` in migrate/seed code). A service that is only IAsyncDisposable throws
// "type only implements IAsyncDisposable" when a sync scope disposes it. Both dispose paths are
// no-ops unless this repository owns its context (factory path).
public class CoreQueryRepository : IQueryRepository, IDisposable
{
    protected ApplicationDbContext DB { get; }
    public ITimeProvider TimeProvider { get; }

    private IScopeLog log;
    private IScopeManager LogManager;
    private readonly bool _ownsContext;

    public CoreQueryRepository(IScopeManager logManager, ITimeProvider timeProvider, ApplicationDbContext dB, bool ownsContext = false)
    {
        LogManager = logManager;
        log = logManager.CreateLogger(this);

        TimeProvider = timeProvider;
        DB = dB;
        _ownsContext = ownsContext;
    }

    // Disposes the context only when this repository created it (factory path, ownsContext: true).
    // The DI-scoped registration passes ownsContext: false — DI owns and disposes that context.
    public async ValueTask DisposeAsync()
    {
        if (_ownsContext)
        {
            await DB.DisposeAsync();
        }
    }

    // Synchronous counterpart for sync scope disposal (see class remark). Same ownership rule.
    public void Dispose()
    {
        if (_ownsContext)
        {
            DB.Dispose();
        }
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

    public IImportSessionQuery GetImportSessionQuery(UserProfile currentProfile)
    {
        return new CoreImportSessionQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public IFeatureFlagQuery GetFeatureFlagQuery(UserProfile currentProfile)
    {
        return new CoreFeatureFlagQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public IApiKeyQuery GetApiKeyQuery(UserProfile currentProfile)
    {
        return new CoreApiKeyQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public IWebhookSubscriptionQuery GetWebhookSubscriptionQuery(UserProfile currentProfile)
    {
        return new CoreWebhookSubscriptionQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public IWebhookDeliveryQuery GetWebhookDeliveryQuery(UserProfile currentProfile)
    {
        return new CoreWebhookDeliveryQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public IRefreshTokenQuery GetRefreshTokenQuery(UserProfile currentProfile)
    {
        return new CoreRefreshTokenQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public IUserOrganizationQuery GetUserOrganizationQuery(UserProfile currentProfile)
    {
        return new CoreUserOrganizationQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public IPermissionQuery GetPermissionQuery(UserProfile currentProfile)
    {
        return new CorePermissionQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public IUserPermissionOverrideQuery GetUserPermissionOverrideQuery(UserProfile currentProfile)
    {
        return new CoreUserPermissionOverrideQuery(LogManager, TimeProvider, DB, currentProfile);
    }

    public IRolePermissionQuery GetRolePermissionQuery(UserProfile currentProfile)
    {
        return new CoreRolePermissionQuery(LogManager, TimeProvider, DB, currentProfile);
    }
}
