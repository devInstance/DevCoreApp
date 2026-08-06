namespace DevInstance.DevCoreApp.Shared.Model.Core.UserAdmin;

public class PermissionOverrideItem
{
    public string PermissionKey { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
    public string? Reason { get; set; }
}
