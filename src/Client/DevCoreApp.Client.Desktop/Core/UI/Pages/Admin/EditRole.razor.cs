using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Client.Services.Core.Roles;
using DevInstance.DevCoreApp.Shared.Model.Core.Roles;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Client.Desktop.Core.UI.Pages.Admin;

/// <summary>
/// Role name, description and permission matrix on a full page — the permission grid outgrew the
/// modal the roles list used to open.
/// </summary>
public partial class EditRole
{
    [Parameter]
    public string RoleId { get; set; } = string.Empty;

    [Inject]
    private IRoleManagementService RoleService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    private RoleItem? Role { get; set; }
    private List<PermissionItem>? allPermissions;
    private HashSet<string> selectedPermissionKeys = new();

    protected override async Task OnParametersSetAsync()
    {
        Role = null;
        allPermissions = null;
        selectedPermissionKeys = new HashSet<string>();

        await Host.ServiceReadAsync(
            async () => await RoleService.GetRoleAsync(RoleId),
            result => Role = result
        );

        if (Host.IsError) return;

        await Host.ServiceReadAsync(
            async () => await RoleService.GetAllPermissionsAsync(),
            result => allPermissions = result
        );

        if (Host.IsError) return;

        await Host.ServiceReadAsync(
            async () => await RoleService.GetRolePermissionKeysAsync(RoleId),
            result => selectedPermissionKeys = new HashSet<string>(result)
        );
    }

    private async Task SaveRoleAndPermissions()
    {
        if (Role == null) return;

        await Host.ServiceSubmitAsync(
            async () => await RoleService.UpdateRoleAsync(Role.Id, Role)
        );

        if (!Host.IsError && !Role.IsSystemRole)
        {
            var request = new RolePermissionsRequest
            {
                PermissionKeys = selectedPermissionKeys.ToList()
            };

            await Host.ServiceSubmitAsync(
                async () => await RoleService.SetRolePermissionsAsync(Role.Id, request)
            );
        }

        if (!Host.IsError)
        {
            NavigationManager.NavigateTo("/admin/roles");
        }
    }
}
