using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Admin.WebService.Services;
using DevInstance.DevCoreApp.Shared.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.User;

public partial class Profile
{
    [Inject]
    private UserProfileService UserService { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    private UserProfileItem? Input { get; set; }

    private string? successMessage;

    protected override async Task OnInitializedAsync()
    {
        await Host.ServiceReadAsync(
            async () => UserService.GetCurrentUser(),
            result => Input = result);
    }

    private async Task UpdateProfile()
    {
        if (Input == null) return;

        successMessage = null;

        await Host.ServiceSubmitAsync(
            async () => await UserService.UpdateCurrentUserAsync(Input),
            result =>
            {
                Input = result;
                successMessage = "Profile updated successfully.";
            });
    }

    private string GetInitials()
    {
        if (Input == null) return "?";

        var initials = "";
        if (!string.IsNullOrWhiteSpace(Input.FirstName))
            initials += Input.FirstName[0];
        if (!string.IsNullOrWhiteSpace(Input.LastName))
            initials += Input.LastName[0];

        return initials.ToUpper();
    }
}
