using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Database.Core.Models.Notifications;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using System;
using System.Linq;

namespace NoCrast.Server.Database.Postgres.Data.Queries;

public class CoreUserNotificationPreferenceQuery : CoreBaseQuery<UserNotificationPreference, CoreUserNotificationPreferenceQuery>, IUserNotificationPreferenceQuery
{
    private CoreUserNotificationPreferenceQuery(IQueryable<UserNotificationPreference> q, IScopeManager logManager,
                         ITimeProvider timeProvider,
                         ApplicationDbContext dB,
                         UserProfile currentProfile)
        : base(q, logManager, timeProvider, dB, currentProfile)
    {
    }

    public CoreUserNotificationPreferenceQuery(IScopeManager logManager,
                             ITimeProvider timeProvider,
                             ApplicationDbContext dB,
                             UserProfile currentProfile)
        : base(logManager, timeProvider, dB, currentProfile)
    {
    }

    public IUserNotificationPreferenceQuery ByPublicId(string id) => ByGuidIdHelper(id);

    public IUserNotificationPreferenceQuery ByUserProfileId(Guid userProfileId)
    {
        currentQuery = from p in currentQuery
                       where p.UserProfileId == userProfileId
                       select p;
        return this;
    }

    public IUserNotificationPreferenceQuery ByCategory(string category)
    {
        currentQuery = from p in currentQuery
                       where p.NotificationCategory == category
                       select p;
        return this;
    }

    public UserNotificationPreference CreateNew()
    {
        return new UserNotificationPreference
        {
            Id = Guid.NewGuid(),
        };
    }

    public IUserNotificationPreferenceQuery Clone()
    {
        return new CoreUserNotificationPreferenceQuery(currentQuery, LogManager, TimeProvider, DB, CurrentProfile);
    }

    public IUserNotificationPreferenceQuery Skip(int value) => SkipHelper(value);

    public IUserNotificationPreferenceQuery Take(int value) => TakeHelper(value);
}
