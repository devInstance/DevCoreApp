using DevInstance.WebServiceToolkit.Common.Model;

namespace DevInstance.DevCoreApp.Shared.Model.Roles;

public class PermissionItem : ModelItem
{
    public string Module { get; set; } = string.Empty;

    public string Entity { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }
}
