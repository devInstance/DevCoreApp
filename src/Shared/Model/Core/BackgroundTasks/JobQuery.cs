using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.WebServiceToolkit.Http.Query;

namespace DevInstance.DevCoreApp.Shared.Model.Core.BackgroundTasks;

/// <summary>Filters for <c>GET api/jobs</c>.</summary>
[QueryModel]
public class JobQuery : DateRangeListQuery
{
    public int? Status { get; set; }
    public string TaskType { get; set; }
}
