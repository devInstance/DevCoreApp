using DevInstance.DevCoreApp.Server.Database.Core.Models.Notifications;
using DevInstance.WebServiceToolkit.Database.Queries;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface INotificationQuery : IModelQuery<Notification, INotificationQuery>,
        IQSearchable<INotificationQuery>,
        IQPageable<INotificationQuery>,
        IQSortable<INotificationQuery>
{
    IQueryable<Notification> Select();

    INotificationQuery ByUserProfileId(Guid userProfileId);
    INotificationQuery ByIsRead(bool isRead);
    INotificationQuery ByGroupKey(string groupKey);
}
