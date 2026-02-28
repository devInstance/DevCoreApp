using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Admin.Services.Organizations;
using DevInstance.DevCoreApp.Server.Admin.Services.Roles;
using DevInstance.DevCoreApp.Server.Admin.Services.UserAdmin;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.DevCoreApp.Shared.Model.Common;
using DevInstance.DevCoreApp.Shared.Model.Organizations;
using DevInstance.DevCoreApp.Shared.Model.Roles;
using DevInstance.DevCoreApp.Shared.Model.UserAdmin;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.Admin;

public partial class EditUser
{
    [Parameter]
    public string UserId { get; set; } = string.Empty;

    [Inject]
    private IUserProfileService UserService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject(Key = null)]
    private IOrganizationService? OrganizationService { get; set; }

    [Inject(Key = null)]
    private IRoleManagementService? RoleManagementService { get; set; }

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    // Profile tab
    private UserProfileItem? Input { get; set; }
    private string SelectedRole { get; set; } = "";
    private string? RoleError { get; set; }
    private List<string> AvailableRoles { get; set; } = new();

    // Tab management
    private string ActiveTab { get; set; } = "profile";
    private HashSet<string> LoadedTabs { get; set; } = new() { "profile" };

    // Organizations tab
    private List<UserOrganizationItem>? UserOrganizations { get; set; }
    private List<OrganizationItem>? AvailableOrganizations { get; set; }
    private bool ShowOrgModal { get; set; }
    private string NewOrgId { get; set; } = "";
    private OrganizationAccessScope NewOrgScope { get; set; } = OrganizationAccessScope.Self;

    // Permission Overrides tab
    private List<PermissionItem>? AllPermissions { get; set; }
    private Dictionary<string, string>? OverrideStates { get; set; } // key → "inherit"|"grant"|"deny"
    private Dictionary<string, Dictionary<string, List<PermissionItem>>>? GroupedAllPermissions { get; set; }
    private Dictionary<string, List<string>> overrideModuleActions = new();

    // Effective Permissions tab
    private List<EffectivePermissionItem>? EffectivePermissions { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await Host.ServiceReadAsync(
            () => Task.FromResult(UserService.GetAvailableRoles()),
            (roles) => AvailableRoles = roles
        );

        await Host.ServiceReadAsync(
            async () => await UserService.GetAsync(UserId),
            (user) =>
            {
                Input = user;
                SelectedRole = user.Roles.Split(',').FirstOrDefault()?.Trim() ?? "";
            }
        );
    }

    private async Task SetActiveTab(string tab)
    {
        ActiveTab = tab;

        if (!LoadedTabs.Contains(tab))
        {
            LoadedTabs.Add(tab);

            switch (tab)
            {
                case "organizations":
                    await LoadOrganizationsTab();
                    break;
                case "overrides":
                    await LoadOverridesTab();
                    break;
                case "effective":
                    await LoadEffectiveTab();
                    break;
            }
        }
    }

    // ── Profile ──

    private async Task UpdateUser()
    {
        RoleError = null;

        if (string.IsNullOrWhiteSpace(SelectedRole))
        {
            RoleError = "Please select a role";
            return;
        }

        if (Input == null) return;

        await Host.ServiceSubmitAsync(
            async () => await UserService.UpdateUserAsync(UserId, Input, SelectedRole)
        );

        if (!Host.IsError)
        {
            NavigationManager.NavigateTo("/admin/users");
        }
    }

    // ── Organizations ──

    private async Task LoadOrganizationsTab()
    {
        await Host.ServiceReadAsync(
            async () => await UserService.GetUserOrganizationsAsync(UserId),
            (orgs) => UserOrganizations = orgs
        );

        if (OrganizationService != null)
        {
            await Host.ServiceReadAsync(
                async () => await OrganizationService.GetTreeAsync(),
                (tree) => AvailableOrganizations = tree
            );
        }
    }

    private void ShowAddOrgModal()
    {
        NewOrgId = "";
        NewOrgScope = OrganizationAccessScope.Self;
        ShowOrgModal = true;
    }

    private void CloseOrgModal()
    {
        ShowOrgModal = false;
    }

    private void AddOrganization()
    {
        if (string.IsNullOrEmpty(NewOrgId) || UserOrganizations == null) return;

        // Don't add duplicates
        if (UserOrganizations.Any(o => o.OrganizationId == NewOrgId)) return;

        var orgItem = AvailableOrganizations?.FirstOrDefault(o => o.Id == NewOrgId);

        var newOrg = new UserOrganizationItem
        {
            OrganizationId = NewOrgId,
            OrganizationName = orgItem?.Name ?? NewOrgId,
            OrganizationPath = orgItem?.Path ?? "",
            Scope = NewOrgScope,
            IsPrimary = UserOrganizations.Count == 0 // First one is primary by default
        };

        UserOrganizations.Add(newOrg);
        ShowOrgModal = false;
    }

    private void RemoveOrg(UserOrganizationItem org)
    {
        if (UserOrganizations == null) return;

        UserOrganizations.Remove(org);

        // If removed org was primary, reassign to first remaining
        if (org.IsPrimary && UserOrganizations.Count > 0)
        {
            UserOrganizations[0].IsPrimary = true;
        }
    }

    private void SetPrimaryOrg(UserOrganizationItem org)
    {
        if (UserOrganizations == null) return;

        foreach (var o in UserOrganizations)
            o.IsPrimary = false;

        org.IsPrimary = true;
    }

    private void OnScopeChanged(UserOrganizationItem org, ChangeEventArgs e)
    {
        if (Enum.TryParse<OrganizationAccessScope>(e.Value?.ToString(), out var scope))
        {
            org.Scope = scope;
        }
    }

    private async Task SaveOrganizations()
    {
        if (UserOrganizations == null) return;

        await Host.ServiceSubmitAsync(
            async () => await UserService.SetUserOrganizationsAsync(UserId, UserOrganizations)
        );
    }

    // ── Permission Overrides ──

    private async Task LoadOverridesTab()
    {
        if (RoleManagementService != null)
        {
            await Host.ServiceReadAsync(
                async () => await RoleManagementService.GetAllPermissionsAsync(),
                (perms) =>
                {
                    AllPermissions = perms;
                    BuildOverrideGroups();
                }
            );
        }

        await Host.ServiceReadAsync(
            async () => await UserService.GetUserPermissionOverridesAsync(UserId),
            (overrides) =>
            {
                OverrideStates ??= new();
                // Initialize all as "inherit"
                if (AllPermissions != null)
                {
                    foreach (var p in AllPermissions)
                    {
                        OverrideStates[p.Key] = "inherit";
                    }
                }
                // Apply existing overrides
                foreach (var ov in overrides)
                {
                    OverrideStates[ov.PermissionKey] = ov.IsGranted ? "grant" : "deny";
                }
            }
        );
    }

    private void BuildOverrideGroups()
    {
        if (AllPermissions == null) return;

        GroupedAllPermissions = AllPermissions
            .GroupBy(p => p.Module)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(p => p.Entity)
                      .OrderBy(eg => eg.Key)
                      .ToDictionary(
                          eg => eg.Key,
                          eg => eg.OrderBy(p => p.DisplayOrder).ToList()));

        overrideModuleActions = AllPermissions
            .GroupBy(p => p.Module)
            .ToDictionary(
                g => g.Key,
                g => g.Select(p => p.Action).Distinct().OrderBy(a => a).ToList());
    }

    private List<string> GetOverrideModuleActions(string module)
    {
        return overrideModuleActions.GetValueOrDefault(module, new List<string>());
    }

    private string GetOverrideState(string key)
    {
        return OverrideStates?.GetValueOrDefault(key, "inherit") ?? "inherit";
    }

    private void OnOverrideChanged(string key, ChangeEventArgs e)
    {
        if (OverrideStates == null) return;
        OverrideStates[key] = e.Value?.ToString() ?? "inherit";
    }

    private async Task SaveOverrides()
    {
        if (OverrideStates == null) return;

        var overrides = OverrideStates
            .Where(kv => kv.Value != "inherit")
            .Select(kv => new PermissionOverrideItem
            {
                PermissionKey = kv.Key,
                IsGranted = kv.Value == "grant"
            })
            .ToList();

        await Host.ServiceSubmitAsync(
            async () => await UserService.SetUserPermissionOverridesAsync(UserId, overrides)
        );

        // Invalidate effective tab cache so it reloads
        if (LoadedTabs.Contains("effective"))
        {
            LoadedTabs.Remove("effective");
            EffectivePermissions = null;
        }
    }

    // ── Effective Permissions ──

    private async Task LoadEffectiveTab()
    {
        await Host.ServiceReadAsync(
            async () => await UserService.GetEffectivePermissionsAsync(UserId),
            (perms) => EffectivePermissions = perms
        );
    }
}
