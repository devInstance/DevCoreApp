namespace DevInstance.DevCoreApp.Shared.Model.UserAdmin;

public class EffectivePermissionItem
{
    public string Key { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
    public string Source { get; set; } = string.Empty;
}
