using System.Text;
using System.Text.Encodings.Web;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.Admin.Services.Core.Account;
using DevInstance.DevCoreApp.Server.Admin.Services.Core.Authentication;
using DevInstance.DevCoreApp.Shared.Model.Core.Account;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Utils.Core;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Core;

/// <summary>
/// Anonymous account flows (<c>api/account</c> and the owner-setup page). Nothing here signs a user
/// in: sign-in is JWT (<see cref="IJwtAuthService"/>). Failures are thrown as WebServiceToolkit
/// exceptions (400), so a returned result succeeded. Email links are built from
/// <see cref="IAccountLinkBuilder"/> and point at the client routes in <see cref="AccountRoutes"/>.
/// </summary>
[BlazorService]
[BlazorServiceMock]
public class AccountService : BaseService, IAccountService
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IUserStore<ApplicationUser> userStore;
    private readonly IEmailSender<ApplicationUser> emailSender;
    private readonly IAccountLinkBuilder linkBuilder;
    private readonly IScopeLog log;

    public AccountService(
        IScopeManager logManager,
        ITimeProvider timeProvider,
        IQueryRepositoryFactory repositoryFactory,
        IAuthorizationContext authorizationContext,
        UserManager<ApplicationUser> userManager,
        IUserStore<ApplicationUser> userStore,
        IEmailSender<ApplicationUser> emailSender,
        IAccountLinkBuilder linkBuilder)
        : base(logManager, timeProvider, repositoryFactory, authorizationContext)
    {
        log = logManager.CreateLogger(this);
        this.userManager = userManager;
        this.userStore = userStore;
        this.emailSender = emailSender;
        this.linkBuilder = linkBuilder;
    }

    // ── Self-registration and emailed links ─────────────────────────────────────────

    public async Task<ServiceActionResult<RegisterResult>> RegisterAsync(RegisterParameters input)
    {
        using var l = log.TraceScope();

        var user = Activator.CreateInstance<ApplicationUser>();
        await userStore.SetUserNameAsync(user, input.Email, CancellationToken.None);
        await ((IUserEmailStore<ApplicationUser>)userStore).SetEmailAsync(user, input.Email, CancellationToken.None);

        var result = await userManager.CreateAsync(user, input.Password);
        ThrowIfFailed(result);
        l.I("User created a new account with password.");

        var userId = await userManager.GetUserIdAsync(user);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(await userManager.GenerateEmailConfirmationTokenAsync(user)));
        var callbackUrl = $"{BuildLink(AccountRoutes.ConfirmEmail)}?userId={userId}&code={code}";
        await emailSender.SendConfirmationLinkAsync(user, input.Email, HtmlEncoder.Default.Encode(callbackUrl));

        // No sign-in here even when confirmation is not required: the client signs in through
        // api/auth once registered.
        return ServiceActionResult<RegisterResult>.OK(
            RegisterResult.Success(userManager.Options.SignIn.RequireConfirmedAccount));
    }

    public async Task<ServiceActionResult<bool>> SendPasswordResetLinkAsync(ForgotPasswordParameters input)
    {
        using var l = log.TraceScope();

        var user = await userManager.FindByEmailAsync(input.Email);
        if (user is not null && await userManager.IsEmailConfirmedAsync(user))
        {
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(await userManager.GeneratePasswordResetTokenAsync(user)));
            var callbackUrl = $"{BuildLink(AccountRoutes.ResetPassword)}?code={code}";
            await emailSender.SendPasswordResetLinkAsync(user, input.Email, HtmlEncoder.Default.Encode(callbackUrl));
            l.I($"Password reset link sent to {input.Email}");
        }

        // Always succeed, so the endpoint cannot be used to find out which emails have accounts.
        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<bool>> ResetPasswordAsync(ResetPasswordParameters input)
    {
        using var l = log.TraceScope();

        var token = TryDecodeCode(input.Code) ?? throw new BadRequestException("Invalid password reset link.");

        var user = await userManager.FindByEmailAsync(input.Email);
        if (user is null)
        {
            // Don't reveal that the user does not exist.
            return ServiceActionResult<bool>.OK(true);
        }

        ThrowIfFailed(await userManager.ResetPasswordAsync(user, token, input.Password));
        l.I($"Password reset for user {input.Email}");
        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<ConfirmEmailResult>> ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        using var l = log.TraceScope();

        // One message for unknown user / malformed or expired token, so the endpoint does not
        // reveal which user ids exist.
        const string invalidLink = "Invalid or expired confirmation link.";

        var token = TryDecodeCode(request.Code) ?? throw new BadRequestException("Invalid confirmation link.");
        var user = await userManager.FindByIdAsync(request.UserId) ?? throw new BadRequestException(invalidLink);

        if (await userManager.IsEmailConfirmedAsync(user))
        {
            return ServiceActionResult<ConfirmEmailResult>.OK(
                ConfirmEmailResult.AlreadyConfirmedResult(request.UserId, needsPassword: !await userManager.HasPasswordAsync(user)));
        }

        if (!(await userManager.ConfirmEmailAsync(user, token)).Succeeded)
        {
            l.E($"Email confirmation failed for user {request.UserId}");
            throw new BadRequestException(invalidLink);
        }

        l.I($"Email confirmed for user {request.UserId}");

        // Users created by an administrator have no password yet; they set it next.
        var needsPassword = !await userManager.HasPasswordAsync(user);
        if (!needsPassword)
        {
            await ActivateProfileIfUsableAsync(user);
        }

        return ServiceActionResult<ConfirmEmailResult>.OK(ConfirmEmailResult.Success(request.UserId, needsPassword));
    }

    public async Task<ServiceActionResult<bool>> SetInvitationPasswordAsync(InvitationPasswordRequest request)
    {
        using var l = log.TraceScope();

        // The endpoint is anonymous: the emailed confirmation token is the only proof that the
        // caller is the invited user. Without this check anyone could set the first password of
        // any invited account that has not chosen one yet.
        var user = await userManager.FindByIdAsync(request.UserId);
        var token = TryDecodeCode(request.Code);
        if (user == null || token == null
            || !await userManager.VerifyUserTokenAsync(user,
                userManager.Options.Tokens.EmailConfirmationTokenProvider,
                UserManager<ApplicationUser>.ConfirmEmailTokenPurpose, token))
        {
            l.W($"Rejected invitation password for user {request.UserId}: invalid link.");
            throw new BadRequestException("Invalid or expired invitation link.");
        }

        ThrowIfFailed(await userManager.AddPasswordAsync(user, request.Password));
        l.I($"Password set for user {request.UserId}");
        await ActivateProfileIfUsableAsync(user);
        return ServiceActionResult<bool>.OK(true);
    }

    // ── First-run owner setup ────────────────────────────────────────────────────────

    public async Task<ServiceActionResult<bool>> IsSetupRequiredAsync() =>
        ServiceActionResult<bool>.OK(!await userManager.Users.AnyAsync());

    public async Task<ServiceActionResult<bool>> SetupOwnerAsync(SetupOwnerParameters input)
    {
        using var l = log.TraceScope();

        // The only gate on this anonymous flow: it works exactly once, before any user exists.
        if (await userManager.Users.AnyAsync())
        {
            throw new ForbiddenException("Setup has already been completed.");
        }

        var user = Activator.CreateInstance<ApplicationUser>();
        await userStore.SetUserNameAsync(user, input.Email, CancellationToken.None);
        await ((IUserEmailStore<ApplicationUser>)userStore).SetEmailAsync(user, input.Email, CancellationToken.None);
        ThrowIfFailed(await userManager.CreateAsync(user, input.Password));
        l.I($"Owner account created with email {input.Email}.");

        var roleResult = await userManager.AddToRoleAsync(user, ApplicationRoles.Owner);
        if (!roleResult.Succeeded)
        {
            l.E($"Failed to assign Owner role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
        }

        await using var repo = RepositoryFactory.Create();
        var profileQuery = repo.GetUserProfilesQuery(null!);
        var userProfile = profileQuery.CreateNew();
        userProfile.Email = input.Email;
        userProfile.FirstName = input.FirstName;
        userProfile.MiddleName = input.MiddleName ?? "";
        userProfile.LastName = input.LastName;
        userProfile.PhoneNumber = input.PhoneNumber ?? "";
        userProfile.ApplicationUserId = user.Id;
        userProfile.Status = UserStatus.LIVE;
        userProfile.TimeZoneId = input.TimeZoneId;
        await profileQuery.AddAsync(userProfile);
        l.I($"UserProfile created for owner with email {input.Email}.");

        await AssignRootOrganizationAsync(repo, user);

        // The owner's address is trusted: they are the one installing the application.
        await userManager.ConfirmEmailAsync(user, await userManager.GenerateEmailConfirmationTokenAsync(user));

        // No sign-in: the owner logs in through the client like everyone else.
        return ServiceActionResult<bool>.OK(true);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Moves the profile to <see cref="UserStatus.LIVE"/> once the account can actually be signed
    /// into. Without this every invited user reads as INITIATED forever, since nothing else in the
    /// codebase ever advances the field.
    /// </summary>
    private async Task ActivateProfileIfUsableAsync(ApplicationUser user)
    {
        using var l = log.TraceScope();

        var emailConfirmed = await userManager.IsEmailConfirmedAsync(user);
        var hasPassword = await userManager.HasPasswordAsync(user);

        await using var repo = RepositoryFactory.Create();
        var query = repo.GetUserProfilesQuery(null!);
        var profile = await query.ByApplicationUserId(user.Id).Select().FirstOrDefaultAsync();

        if (profile == null)
        {
            // Self-registration creates no profile. Nothing to advance.
            return;
        }

        if (!UserActivation.ShouldActivate(profile.Status, emailConfirmed, hasPassword))
        {
            return;
        }

        profile.Status = UserStatus.LIVE;
        await query.UpdateAsync(profile);

        l.I($"UserProfile {profile.PublicId} advanced to LIVE.");
    }

    /// <summary>
    /// Puts the owner in the seeded root organization.
    /// <para>
    /// Without this the very first account has no <c>UserOrganizations</c> row, which does not
    /// merely restrict them — it breaks in both directions at once. The global query filter is
    /// fail-open on an empty visible set, so they read every organization, while
    /// <c>OrganizationStampInterceptor</c> throws on every organization-scoped insert. The owner
    /// could see everything and create nothing.
    /// </para>
    /// </summary>
    private async Task AssignRootOrganizationAsync(IQueryRepository repo, ApplicationUser user)
    {
        using var l = log.TraceScope();

        // OrganizationDataSeeder runs at startup, so the root org exists by now. The seeder creates
        // one tenant whose RootOrganizationId is the single parentless organization.
        var rootOrganizationId = await repo.GetOrganizationsQuery(null!)
            .ByParentId(null)
            .Select()
            .OrderBy(o => o.SortOrder)
            .Select(o => (Guid?)o.Id)
            .FirstOrDefaultAsync();

        if (rootOrganizationId == null || rootOrganizationId == Guid.Empty)
        {
            l.E("Owner setup could not find a root organization to assign. The owner will read every "
                + "organization and be unable to create records until assigned one manually.");
            return;
        }

        var userOrgQuery = repo.GetUserOrganizationQuery(null!);
        var assignment = userOrgQuery.CreateNew();
        assignment.UserId = user.Id;
        assignment.OrganizationId = rootOrganizationId.Value;
        // WithChildren, not Self: the owner is the top of the tree and every organization added
        // later hangs beneath the root. Self would hide each new one until assigned by hand.
        assignment.Scope = OrganizationAccessScope.WithChildren;
        assignment.IsPrimary = true;
        await userOrgQuery.AddAsync(assignment);

        user.PrimaryOrganizationId = rootOrganizationId;
        await userManager.UpdateAsync(user);

        l.I($"Owner assigned to root organization {rootOrganizationId}.");
    }

    private string BuildLink(string route)
    {
        var origin = linkBuilder.TryBuildBase()
            ?? throw new InvalidOperationException($"Cannot build account links: set {AccountLinkBuilder.BaseUrlKey}.");
        return origin + route;
    }

    private static string? TryDecodeCode(string? encoded)
    {
        if (string.IsNullOrEmpty(encoded)) return null;
        try { return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encoded)); }
        catch (FormatException) { return null; }
    }

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }
}
