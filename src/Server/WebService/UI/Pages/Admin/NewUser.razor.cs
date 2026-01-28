using System.ComponentModel.DataAnnotations;
using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.WebService.Services;
using DevInstance.DevCoreApp.Shared.Model;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Server.WebService.UI.Pages.Admin;

public partial class NewUser
{
    [Inject]
    private UserProfileService UserService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    private List<string> AvailableRoles { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        Input ??= new();

        await Host.ServiceReadAsync(
            () => Task.FromResult(UserService.GetAvailableRoles()),
            (roles) => AvailableRoles = roles
        );
    }

    private async Task CreateUser()
    {
        var newUser = new UserProfileItem
        {
            FirstName = Input.FirstName,
            MiddleName = Input.MiddleName ?? "",
            LastName = Input.LastName,
            PhoneNumber = Input.PhoneNumber ?? "",
            Email = Input.Email
        };

        await Host.ServiceSubmitAsync(
            async () => await UserService.CreateUserAsync(newUser, Input.Role)
        );

        if (!Host.IsError)
        {
            NavigationManager.NavigateTo("/admin/users");
        }
    }

    private sealed class InputModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "";

        [Display(Name = "Middle Name")]
        public string? MiddleName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = "";

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; } = "";
    }
}
