using DevInstance.DevCoreApp.Server.Database.Core.Models.Notifications;
using DevInstance.WebServiceToolkit.Database.Queries;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IUserNotificationPreferenceQuery : IModelQuery<UserNotificationPreference, IUserNotificationPreferenceQuery>,
        IQPageable<IUserNotificationPreferenceQuery>
{
    IQueryable<UserNotificationPreference> Select();

    IUserNotificationPreferenceQuery ByUserProfileId(Guid userProfileId);
    IUserNotificationPreferenceQuery ByCategory(string category);
}
