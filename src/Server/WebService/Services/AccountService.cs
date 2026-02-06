using System.Text;
using System.Text.Encodings.Web;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Utils;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.WebService.Authentication;
using DevInstance.DevCoreApp.Server.WebService.Tools;
using DevInstance.DevCoreApp.Shared.Model.Account;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace DevInstance.DevCoreApp.Server.WebService.Services;

[AppService]
public class AccountService : BaseService
{
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IUserStore<ApplicationUser> userStore;
    private readonly IEmailSender<ApplicationUser> emailSender;
    private readonly ApplicationDbContext dbContext;
    private readonly IScopeLog log;

    public AccountService(
        IScopeManager logManager,
        ITimeProvider timeProvider,
        IQueryRepository query,
        IAuthorizationContext authorizationContext,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IUserStore<ApplicationUser> userStore,
        IEmailSender<ApplicationUser> emailSender,
        ApplicationDbContext dbContext)
        : base(logManager, timeProvider, query, authorizationContext)
    {
        log = logManager.CreateLogger(this);
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.userStore = userStore;
        this.emailSender = emailSender;
        this.dbContext = dbContext;
    }

    public async Task<LoginResult> LoginAsync(LoginParameters input)
    {
        using var l = log.TraceScope();

        var result = await signInManager.PasswordSignInAsync(
            input.Email,
            input.Password,
            input.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            l.I("User logged in.");
            return LoginResult.Success();
        }

        if (result.IsLockedOut)
        {
            l.W("User account locked out.");
            return LoginResult.LockedOut();
        }

        return LoginResult.InvalidLogin();
    }

    public async Task<RegisterResult> RegisterAsync(RegisterParameters input, string confirmationLinkBase)
    {
        using var l = log.TraceScope();

        var user = Activator.CreateInstance<ApplicationUser>();

        await userStore.SetUserNameAsync(user, input.Email, CancellationToken.None);
        var emailStore = (IUserEmailStore<ApplicationUser>)userStore;
        await emailStore.SetEmailAsync(user, input.Email, CancellationToken.None);
        var result = await userManager.CreateAsync(user, input.Password);

        if (!result.Succeeded)
        {
            return RegisterResult.Failed(result.Errors.Select(e => e.Description));
        }

        l.I("User created a new account with password.");

        var userId = await userManager.GetUserIdAsync(user);
        var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = $"{confirmationLinkBase}?userId={userId}&code={code}";

        await emailSender.SendConfirmationLinkAsync(user, input.Email, HtmlEncoder.Default.Encode(callbackUrl));

        var requiresConfirmation = userManager.Options.SignIn.RequireConfirmedAccount;

        if (!requiresConfirmation)
        {
            await signInManager.SignInAsync(user, isPersistent: false);
        }

        return RegisterResult.Success(requiresConfirmation);
    }

    public async Task SendPasswordResetLinkAsync(ForgotPasswordParameters input, string resetLinkBase)
    {
        using var l = log.TraceScope();

        var user = await userManager.FindByEmailAsync(input.Email);
        if (user is not null && await userManager.IsEmailConfirmedAsync(user))
        {
            var code = await userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = $"{resetLinkBase}?code={code}";

            await emailSender.SendPasswordResetLinkAsync(user, input.Email, HtmlEncoder.Default.Encode(callbackUrl));
            l.I($"Password reset link sent to {input.Email}");
        }

        // Always complete successfully to prevent user enumeration
    }

    public string DecodeResetCode(string encodedCode)
    {
        return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedCode));
    }

    public async Task<ResetPasswordResult> ResetPasswordAsync(ResetPasswordParameters input)
    {
        using var l = log.TraceScope();

        var user = await userManager.FindByEmailAsync(input.Email);
        if (user is null)
        {
            // Don't reveal that the user does not exist - show success anyway
            return ResetPasswordResult.Success();
        }

        var result = await userManager.ResetPasswordAsync(user, input.Code, input.Password);
        if (result.Succeeded)
        {
            l.I($"Password reset for user {input.Email}");
            return ResetPasswordResult.Success();
        }

        return ResetPasswordResult.Failed(result.Errors.Select(e => e.Description));
    }

    public async Task<ConfirmEmailResult> ConfirmEmailAsync(string? userId, string? code)
    {
        using var l = log.TraceScope();

        if (userId is null || code is null)
        {
            return ConfirmEmailResult.InvalidLink();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ConfirmEmailResult.UserNotFound();
        }

        // Check if email is already confirmed
        if (await userManager.IsEmailConfirmedAsync(user))
        {
            return ConfirmEmailResult.AlreadyConfirmedResult(userId);
        }

        // Confirm the email
        var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var result = await userManager.ConfirmEmailAsync(user, decodedCode);

        if (!result.Succeeded)
        {
            l.E($"Email confirmation failed for user {userId}");
            return ConfirmEmailResult.Failed();
        }

        l.I($"Email confirmed for user {userId}");

        // Check if user has a password set (users created by admin may not have set their own password yet)
        var needsPassword = !await userManager.HasPasswordAsync(user);

        return ConfirmEmailResult.Success(userId, needsPassword);
    }

    public async Task<SetPasswordResult> SetPasswordAsync(string userId, SetPasswordParameters input)
    {
        using var l = log.TraceScope();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return SetPasswordResult.Failed(new[] { "User not found." });
        }

        var result = await userManager.AddPasswordAsync(user, input.Password);

        if (result.Succeeded)
        {
            l.I($"Password set for user {userId}");
            return SetPasswordResult.Success();
        }

        return SetPasswordResult.Failed(result.Errors.Select(e => e.Description));
    }

    public async Task<bool> HasUsersAsync()
    {
        return await userManager.Users.AnyAsync();
    }

    public async Task<SetupOwnerResult> SetupOwnerAsync(SetupOwnerParameters input)
    {
        using var l = log.TraceScope();

        // Security check: don't allow setup if users already exist
        if (await userManager.Users.AnyAsync())
        {
            return SetupOwnerResult.AlreadySetup();
        }

        // Create ApplicationUser
        var user = Activator.CreateInstance<ApplicationUser>();

        await userStore.SetUserNameAsync(user, input.Email, CancellationToken.None);
        var emailStore = (IUserEmailStore<ApplicationUser>)userStore;
        await emailStore.SetEmailAsync(user, input.Email, CancellationToken.None);
        var result = await userManager.CreateAsync(user, input.Password);

        if (!result.Succeeded)
        {
            return SetupOwnerResult.Failed(result.Errors.Select(e => e.Description));
        }

        l.I($"Owner account created with email {input.Email}.");

        // Assign Owner role
        var roleResult = await userManager.AddToRoleAsync(user, ApplicationRoles.Owner);
        if (!roleResult.Succeeded)
        {
            l.E($"Failed to assign Owner role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
        }

        // Create UserProfile
        var now = TimeProvider.CurrentTime;
        var userProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            PublicId = IdGenerator.New(),
            Email = input.Email,
            FirstName = input.FirstName,
            MiddleName = input.MiddleName ?? "",
            LastName = input.LastName,
            PhoneNumber = input.PhoneNumber ?? "",
            ApplicationUserId = user.Id,
            Status = UserStatus.LIVE,
            CreateDate = now,
            UpdateDate = now
        };
        dbContext.UserProfiles.Add(userProfile);
        await dbContext.SaveChangesAsync();

        l.I($"UserProfile created for owner with email {input.Email}.");

        // Automatically confirm email for owner account during setup
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await userManager.ConfirmEmailAsync(user, token);

        // Sign in the owner
        await signInManager.SignInAsync(user, isPersistent: false);

        return SetupOwnerResult.Success();
    }
}
