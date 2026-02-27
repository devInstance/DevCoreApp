using DevInstance.DevCoreApp.Server.Database.Core.Models.Notifications;
using DevInstance.WebServiceToolkit.Database.Queries;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface INotificationTemplateQuery : IModelQuery<NotificationTemplate, INotificationTemplateQuery>,
        IQSearchable<INotificationTemplateQuery>,
        IQPageable<INotificationTemplateQuery>,
        IQSortable<INotificationTemplateQuery>
{
    IQueryable<NotificationTemplate> Select();

    INotificationTemplateQuery ByCategory(string category);
    INotificationTemplateQuery ByName(string name);
}
