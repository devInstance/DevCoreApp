using DevInstance.WebServiceToolkit.Http.Query;

namespace DevInstance.DevCoreApp.Shared.Model.Core.Settings;

/// <summary>Filters for <c>GET api/settings</c> (not paged).</summary>
[QueryModel]
public class SettingQuery
{
    /// <summary>Setting scope, e.g. "System", "Organization", "User".</summary>
    public string Scope { get; set; }
    public string OrganizationId { get; set; }
    public string Search { get; set; }
}
