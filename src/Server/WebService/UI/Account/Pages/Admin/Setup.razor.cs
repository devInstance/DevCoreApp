using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using DevInstance.DevCoreApp.Server.WebService.Authentication;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.BlazorToolkit.Utils;

namespace DevInstance.DevCoreApp.Server.WebService.UI.Account.Pages.Admin;

public partial class Setup
{
    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private IUserStore<ApplicationUser> UserStore { get; set; } = default!;

    [Inject]
    private SignInManager<ApplicationUser> SignInManager { get; set; } = default!;

    [Inject]
    private IEmailSender<ApplicationUser> EmailSender { get; set; } = default!;

    [Inject]
    private ILogger<Setup> Logger { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IdentityRedirectManager RedirectManager { get; set; } = default!;

    [Inject]
    private ApplicationDbContext DbContext { get; set; } = default!;

    [Inject]
    private ITimeProvider TimeProvider { get; set; } = default!;

    private IEnumerable<IdentityError>? identityErrors;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = default!;

    private string? Message => identityErrors is null ? null : $"Error: {string.Join(", ", identityErrors.Select(error => error.Description))}";

    protected override async Task OnInitializedAsync()
    {
        Input ??= new();

        // Security check: redirect if users already exist
        if (await UserManager.Users.AnyAsync())
        {
            RedirectManager.RedirectTo("/");
        }
    }

    public async Task RegisterOwner(EditContext editContext)
    {
        // Double-check on submit to prevent race conditions
        if (await UserManager.Users.AnyAsync())
        {
            RedirectManager.RedirectTo("/");
            return;
        }

        var user = CreateUser();

        await UserStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
        var emailStore = GetEmailStore();
        await emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
        var result = await UserManager.CreateAsync(user, Input.Password);

        if (!result.Succeeded)
        {
            identityErrors = result.Errors;
            return;
        }

        Logger.LogInformation("Owner account created with email {Email}.", Input.Email);

        // Assign Owner role
        var roleResult = await UserManager.AddToRoleAsync(user, ApplicationRoles.Owner);
        if (!roleResult.Succeeded)
        {
            Logger.LogError("Failed to assign Owner role: {Errors}", string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }

        // Create UserProfile
        var now = TimeProvider.CurrentTime;
        var userProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            PublicId = IdGenerator.New(),
            Email = Input.Email,
            FirstName = Input.FirstName,
            MiddleName = Input.MiddleName ?? "",
            LastName = Input.LastName,
            PhoneNumber = Input.PhoneNumber ?? "",
            ApplicationUserId = user.Id,
            Status = UserStatus.LIVE,
            CreateDate = now,
            UpdateDate = now
        };
        DbContext.UserProfiles.Add(userProfile);
        await DbContext.SaveChangesAsync();

        Logger.LogInformation("UserProfile created for owner with email {Email}.", Input.Email);

        var userId = await UserManager.GetUserIdAsync(user);
        var code = await UserManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = NavigationManager.GetUriWithQueryParameters(
            NavigationManager.ToAbsoluteUri("Account/ConfirmEmail").AbsoluteUri,
            new Dictionary<string, object?> { ["userId"] = userId, ["code"] = code });

        await EmailSender.SendConfirmationLinkAsync(user, Input.Email, HtmlEncoder.Default.Encode(callbackUrl));

        if (UserManager.Options.SignIn.RequireConfirmedAccount)
        {
            RedirectManager.RedirectTo(
                "Account/RegisterConfirmation",
                new() { ["email"] = Input.Email });
        }
        else
        {
            await SignInManager.SignInAsync(user, isPersistent: false);
            RedirectManager.RedirectTo("/");
        }
    }

    private ApplicationUser CreateUser()
    {
        try
        {
            return Activator.CreateInstance<ApplicationUser>();
        }
        catch
        {
            throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor.");
        }
    }

    private IUserEmailStore<ApplicationUser> GetEmailStore()
    {
        if (!UserManager.SupportsUserEmail)
        {
            throw new NotSupportedException("The default UI requires a user store with email support.");
        }
        return (IUserEmailStore<ApplicationUser>)UserStore;
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
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
