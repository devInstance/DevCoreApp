using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.WebServiceToolkit.Http.Query;

namespace DevInstance.DevCoreApp.Shared.Model.Core.Organizations;

/// <summary>Filters for <c>GET api/organizations</c>.</summary>
[QueryModel]
public class OrganizationQuery : ListQuery
{
    public bool? IsActive { get; set; }
}
