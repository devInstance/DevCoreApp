using DevInstance.WebServiceToolkit.Common.Model;

namespace DevInstance.DevCoreApp.Shared.Model.Settings;

public class SettingItem : ModelItem
{
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string ValueType { get; set; } = "string";
    public string? Description { get; set; }
    public bool IsSensitive { get; set; }

    /// <summary>
    /// Scope tier: "System", "Tenant", "Organization", or "User".
    /// </summary>
    public string Scope { get; set; } = "System";

    public string? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
}
