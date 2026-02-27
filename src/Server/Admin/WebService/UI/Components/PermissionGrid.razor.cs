using DevInstance.DevCoreApp.Shared.Model.Roles;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Components;

public partial class PermissionGrid
{
    [Parameter]
    public List<PermissionItem>? AllPermissions { get; set; }

    [Parameter]
    public HashSet<string> SelectedKeys { get; set; } = new();

    [Parameter]
    public EventCallback<HashSet<string>> SelectedKeysChanged { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    // Module → Entity → List<PermissionItem>
    private Dictionary<string, Dictionary<string, List<PermissionItem>>>? groupedPermissions;

    // Module → distinct actions
    private Dictionary<string, List<string>> moduleActions = new();

    protected override void OnParametersSet()
    {
        if (AllPermissions != null)
        {
            groupedPermissions = AllPermissions
                .GroupBy(p => p.Module)
                .OrderBy(g => g.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(p => p.Entity)
                          .OrderBy(eg => eg.Key)
                          .ToDictionary(
                              eg => eg.Key,
                              eg => eg.OrderBy(p => p.DisplayOrder).ToList()));

            moduleActions = AllPermissions
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

    private bool IsSelected(string key) => SelectedKeys.Contains(key);

    private bool IsModuleFullySelected(string module)
    {
        if (AllPermissions == null) return false;
        var moduleKeys = AllPermissions.Where(p => p.Module == module).Select(p => p.Key);
        return moduleKeys.All(k => SelectedKeys.Contains(k));
    }

    private async Task OnTogglePermission(string key, bool isChecked)
    {
        if (isChecked)
            SelectedKeys.Add(key);
        else
            SelectedKeys.Remove(key);

        await SelectedKeysChanged.InvokeAsync(SelectedKeys);
    }

    private async Task OnToggleModule(string module, bool isChecked)
    {
        if (AllPermissions == null) return;

        var moduleKeys = AllPermissions.Where(p => p.Module == module).Select(p => p.Key);

        if (isChecked)
        {
            foreach (var key in moduleKeys)
                SelectedKeys.Add(key);
        }
        else
        {
            foreach (var key in moduleKeys)
                SelectedKeys.Remove(key);
        }

        await SelectedKeysChanged.InvokeAsync(SelectedKeys);
    }
}
