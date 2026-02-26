using DevInstance.DevCoreApp.Server.Database.Core.Models.BackgroundTasks;
using DevInstance.DevCoreApp.Shared.Model.Common;
using DevInstance.WebServiceToolkit.Database.Queries;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IBackgroundTaskLogQuery : IModelQuery<BackgroundTaskLog, IBackgroundTaskLogQuery>,
        IQPageable<IBackgroundTaskLogQuery>,
        IQSortable<IBackgroundTaskLogQuery>
{
    IQueryable<BackgroundTaskLog> Select();

    IBackgroundTaskLogQuery ByBackgroundTaskId(Guid backgroundTaskId);
    IBackgroundTaskLogQuery ByStatus(BackgroundTaskLogStatus status);
}
