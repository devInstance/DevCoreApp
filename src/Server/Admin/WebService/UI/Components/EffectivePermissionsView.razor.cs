using DevInstance.DevCoreApp.Shared.Model.UserAdmin;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Components;

public partial class EffectivePermissionsView
{
    [Parameter]
    public List<EffectivePermissionItem>? Permissions { get; set; }

    private Dictionary<string, Dictionary<string, List<EffectivePermissionItem>>>? groupedPermissions;
    private Dictionary<string, List<string>> moduleActions = new();

    protected override void OnParametersSet()
    {
        if (Permissions != null)
        {
            groupedPermissions = Permissions
                .GroupBy(p => p.Module)
                .OrderBy(g => g.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(p => p.Entity)
                          .OrderBy(eg => eg.Key)
                          .ToDictionary(
                              eg => eg.Key,
                              eg => eg.OrderBy(p => p.Key).ToList()));

            moduleActions = Permissions
                .GroupBy(p => p.Module)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(p => p.Action).Distinct().OrderBy(a => a).ToList());
        }
    }

    private List<string> GetModuleActions(string module)
    {
        return moduleActions.GetValueOrDefault(module, new List<string>());
    }
}
