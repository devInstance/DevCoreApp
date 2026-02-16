using System.ComponentModel.DataAnnotations;
using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Admin.Services.UserAdmin;
using DevInstance.DevCoreApp.Shared.Model;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.Admin;

public partial class NewUser
{
    [Inject]
    private IUserProfileService UserService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    [SupplyParameterFromForm]
    private UserProfileItem Input { get; set; } = new();

    [SupplyParameterFromForm(Name = "SelectedRole")]
    private string SelectedRole { get; set; } = "";

    private List<string> AvailableRoles { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        Input ??= new();

        await Host.ServiceReadAsync(
            () => Task.FromResult(UserService.GetAvailableRoles()),
            (roles) => AvailableRoles = roles
        );
    }

    private string? RoleError { get; set; }

    private async Task CreateUser()
    {
        RoleError = null;

        if (string.IsNullOrWhiteSpace(SelectedRole))
        {
            RoleError = "Please select a role";
            return;
        }

        await Host.ServiceSubmitAsync(
            async () => await UserService.CreateUserAsync(Input, SelectedRole)
        );

        if (!Host.IsError)
        {
            NavigationManager.NavigateTo("/admin/users");
        }
    }
}
