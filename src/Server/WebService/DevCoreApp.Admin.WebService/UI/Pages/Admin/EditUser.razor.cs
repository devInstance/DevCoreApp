using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Admin.WebService.Services;
using DevInstance.DevCoreApp.Shared.Model;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.Admin;

public partial class EditUser
{
    [Parameter]
    public string UserId { get; set; } = string.Empty;

    [Inject]
    private UserProfileService UserService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    private UserProfileItem? Input { get; set; }

    private string SelectedRole { get; set; } = "";
    private string? RoleError { get; set; }

    private List<string> AvailableRoles { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await Host.ServiceReadAsync(
            () => Task.FromResult(UserService.GetAvailableRoles()),
            (roles) => AvailableRoles = roles
        );

        await Host.ServiceReadAsync(
            async () => await UserService.GetUserByIdAsync(UserId),
            (user) =>
            {
                Input = user;
                SelectedRole = user.Roles.Split(',').FirstOrDefault()?.Trim() ?? "";
            }
        );
    }

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
}
