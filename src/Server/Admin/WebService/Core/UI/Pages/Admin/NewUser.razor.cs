using System.ComponentModel.DataAnnotations;
using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Admin.Services.Core.Organizations;
using DevInstance.DevCoreApp.Server.Admin.Services.Core.UserAdmin;
using DevInstance.DevCoreApp.Shared.Model.Core;
using DevInstance.DevCoreApp.Shared.Model.Core.Organizations;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Core.UI.Pages.Admin;

public partial class NewUser
{
    [Inject]
    private IUserProfileService UserService { get; set; } = default!;

    [Inject]
    private IOrganizationService OrganizationService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    [SupplyParameterFromForm]
    private UserProfileItem Input { get; set; } = new();

    [SupplyParameterFromForm(Name = "SelectedRole")]
    private string SelectedRole { get; set; } = "";

    private List<string> AvailableRoles { get; set; } = new();

    [SupplyParameterFromForm(Name = "SelectedOrganizationId")]
    private string SelectedOrganizationId { get; set; } = "";

    /// <summary>
    /// Flat and already ordered by materialized Path — GetTreeAsync sorts, it does not nest — so
    /// Level is all the picker needs to indent by.
    /// </summary>
    private List<OrganizationItem> AvailableOrganizations { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        Input ??= new();

        await Host.BeginServiceCall()
            .DispatchCall(
                () => Task.FromResult(UserService.GetAvailableRoles()),
                (roles) => AvailableRoles = roles)
            .DispatchCall(
                async () => await OrganizationService.GetTreeAsync(),
                (orgs) => AvailableOrganizations = orgs ?? new())
            .DispatchCall(
                async () => await OrganizationService.GetCurrentAsync(),
                (current) =>
                {
                    // Default to the creating administrator's own organization. Anything is better
                    // than no assignment, which reads every organization and writes to none.
                    if (string.IsNullOrEmpty(SelectedOrganizationId) && current != null)
                    {
                        SelectedOrganizationId = current.Id;
                    }
                })
            .ExecuteAsync();
    }

    private string? RoleError { get; set; }

    /// <summary>Empty selection means "use the creating administrator's primary organization".</summary>
    private string? OrganizationIdOrNull =>
        string.IsNullOrWhiteSpace(SelectedOrganizationId) ? null : SelectedOrganizationId;

    private async Task CreateUser()
    {
        RoleError = null;

        if (string.IsNullOrWhiteSpace(SelectedRole))
        {
            RoleError = "Please select a role";
            return;
        }

        string? createdUserId = null;

        await Host.ServiceSubmitAsync(
            async () =>
            {
                var result = await UserService.CreateUserAsync(Input, SelectedRole, OrganizationIdOrNull);
                createdUserId = result.Result?.Id;
                return result;
            }
        );

        if (!Host.IsError && !string.IsNullOrEmpty(createdUserId))
        {
            NavigationManager.NavigateTo($"/admin/users/{createdUserId}/edit");
        }
    }
}
