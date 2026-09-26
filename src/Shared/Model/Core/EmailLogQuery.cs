using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.WebServiceToolkit.Http.Query;

namespace DevInstance.DevCoreApp.Shared.Model.Core;

/// <summary>
/// Filters for <c>GET api/email-logs</c>; <c>POST api/email-logs/resend-failed</c> takes the same
/// filters (paging and sorting are ignored there).
/// </summary>
[QueryModel]
public class EmailLogQuery : DateRangeListQuery
{
    public int? Status { get; set; }
    public string TemplateName { get; set; }
}
