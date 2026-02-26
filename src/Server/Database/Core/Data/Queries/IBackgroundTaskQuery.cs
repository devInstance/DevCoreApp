using DevInstance.DevCoreApp.Server.Database.Core.Models.BackgroundTasks;
using DevInstance.DevCoreApp.Shared.Model.Common;
using DevInstance.WebServiceToolkit.Database.Queries;
using System;
using System.Linq;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Queries;

public interface IBackgroundTaskQuery : IModelQuery<BackgroundTask, IBackgroundTaskQuery>,
        IQSearchable<IBackgroundTaskQuery>,
        IQPageable<IBackgroundTaskQuery>,
        IQSortable<IBackgroundTaskQuery>
{
    IQueryable<BackgroundTask> Select();

    IBackgroundTaskQuery ByStatus(BackgroundTaskStatus status);
    IBackgroundTaskQuery ByTaskType(string taskType);
    IBackgroundTaskQuery ByDateRange(DateTime? start, DateTime? end);
    IBackgroundTaskQuery ByCreatedById(Guid createdById);
}
