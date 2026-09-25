using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Core.Account;
using DevInstance.DevCoreApp.Server.Admin.Services.Core.Authentication;
using DevInstance.DevCoreApp.Server.Admin.Services.Core.Background;
using DevInstance.DevCoreApp.Server.Admin.Services.Core.Background.Requests;
using DevInstance.DevCoreApp.Server.Admin.Services.Core.Exceptions;
using DevInstance.DevCoreApp.Server.Admin.Services.Core.Files;
using DevInstance.DevCoreApp.Server.Admin.Services.Core.Notifications.Templates;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.EmailProcessor.Core;
using DevInstance.DevCoreApp.Shared.Model.Core;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.DevCoreApp.Shared.Model.Core.UserAdmin;
using DevInstance.DevCoreApp.Shared.Utils.Core;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;
using DevInstance.WebServiceToolkit.Database.Queries.Extensions;
using DevInstance.WebServiceToolkit.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text;
using SkiaSharp;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Core.UserAdmin;

[BlazorService]
public class UserProfileService : BaseService, IUserProfileService
{
    public UserManager<ApplicationUser> UserManager { get; }
    private IUserStore<ApplicationUser> UserStore { get; }
    private IBackgroundWorker BackgroundWorker { get; }
    private IEmailTemplateService EmailTemplateService { get; }
    private IOrganizationContextResolver OrgResolver { get; }
    private IOperationContext OperationContext { get; }
    private IAccountLinkBuilder LinkBuilder { get; }
    private IConfiguration Configuration { get; }

    private IScopeLog log;

    public UserProfileService(IScopeManager logManager,
                              ITimeProvider timeProvider,
                              IQueryRepositoryFactory repositoryFactory,
                              IAuthorizationContext authorizationContext,
                              UserManager<ApplicationUser> userManager,
                              IUserStore<ApplicationUser> userStore,
                              IBackgroundWorker backgroundWorker,
                              IEmailTemplateService emailTemplateService,
                              IOrganizationContextResolver orgResolver,
                              IOperationContext operationContext,
                              IAccountLinkBuilder linkBuilder,
                              IConfiguration configuration)
        : base(logManager, timeProvider, repositoryFactory, authorizationContext)
    {
        log = logManager.CreateLogger(this);

        UserManager = userManager;
        UserStore = userStore;
        BackgroundWorker = backgroundWorker;
        EmailTemplateService = emailTemplateService;
        OrgResolver = orgResolver;
        OperationContext = operationContext;
        LinkBuilder = linkBuilder;
        Configuration = configuration;
    }

    public ServiceActionResult<UserProfileItem> GetCurrentUser()
    {
        return ServiceActionResult<UserProfileItem>.OK(AuthorizationContext.CurrentProfile.ToView());
    }

    public async Task<ServiceActionResult<UserProfileItem>> UpdateCurrentUserAsync(UserProfileItem newProfile)
    {
        var profile = AuthorizationContext.CurrentProfile;
        profile.ToRecord(newProfile);
        await using var repo = RepositoryFactory.Create();
        await repo.GetUserProfilesQuery(AuthorizationContext.CurrentProfile).UpdateAsync(profile);

        return ServiceActionResult<UserProfileItem>.OK(profile.ToView());
    }

    public async Task<ServiceActionResult<ModelList<UserProfileItem>>> GetListAsync(int? top, int? page, string[] sortBy = null, string search = null)
    {
        using var l = log.TraceScope();

        await using var repo = RepositoryFactory.Create();
        var profilesQuery = repo.GetUserProfilesQuery(AuthorizationContext.CurrentProfile);

        if (!string.IsNullOrEmpty(search))
        {
            profilesQuery = profilesQuery.Search(search);
        }

        if (sortBy != null && sortBy.Length > 0)
        {
            foreach (var sortField in sortBy)
            {
                var isAsc = !sortField.StartsWith("-");
                var field = isAsc ? sortField : sortField.Substring(1);
                profilesQuery = profilesQuery.SortBy(field, isAsc);
            }
        }

        var totalCount = await profilesQuery.Clone().Select().CountAsync();
        var userProfiles = await profilesQuery.Paginate(top, page).Select().ToListAsync();

        var organizationNames = await LoadPrimaryOrganizationNamesAsync(repo, userProfiles);

        var users = new List<UserProfileItem>();

        foreach (var profile in userProfiles)
        {
            var appUser = await UserManager.FindByIdAsync(profile.ApplicationUserId.ToString());

            if (appUser != null)
            {
                var roles = await UserManager.GetRolesAsync(appUser);
                organizationNames.TryGetValue(profile.ApplicationUserId, out var organizationName);
                var newUserViewModel = profile.ToView(appUser, roles, organizationName);

                users.Add(newUserViewModel);
            }
        }

        var modelList = ModelListResult.CreateList(users.ToArray(), totalCount, top, page, sortBy, search, true);
        return ServiceActionResult<ModelList<UserProfileItem>>.OK(modelList);
    }

    /// <summary>
    /// Primary organization name per ApplicationUser id, for one page of results.
    /// <para>
    /// Batched deliberately: the loop around it already pays an N+1 to Identity for roles, and this
    /// column is hidden by default — it must not add a second query per row for a column most
    /// installations never switch on.
    /// </para>
    /// </summary>
    private async Task<Dictionary<Guid, string>> LoadPrimaryOrganizationNamesAsync(IQueryRepository repo, List<UserProfile> profiles)
    {
        if (profiles.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var appUserIds = profiles.Select(p => p.ApplicationUserId).Distinct().ToList();

        var rows = await repo.GetUserOrganizationQuery(AuthorizationContext.CurrentProfile)
            .Select()
            .Where(uo => appUserIds.Contains(uo.UserId) && uo.IsPrimary && uo.Organization != null)
            .Select(uo => new { uo.UserId, uo.Organization!.Name })
            .ToListAsync();

        // GroupBy rather than ToDictionaryAsync: exactly one primary is a rule SetUserOrganizationsAsync
        // enforces on write, not a database constraint, so a legacy row pair must not throw here.
        return rows
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.First().Name);
    }

    public ServiceActionResult<List<string>> GetAvailableRoles()
    {
        return ServiceActionResult<List<string>>.OK(new List<string>
        {
            ApplicationRoles.Admin,
            ApplicationRoles.Manager,
            ApplicationRoles.Employee,
            ApplicationRoles.Client
        });
    }

    public async Task<ServiceActionResult<UserProfileItem>> GetAsync(string id)
    {
        return await GetUserByIdAsync(id);
    }

    public Task<ServiceActionResult<UserProfileItem>> AddAsync(UserProfileItem item)
    {
        throw new NotImplementedException("Use CreateUserAsync with a role parameter instead.");
    }

    public Task<ServiceActionResult<UserProfileItem>> UpdateAsync(string id, UserProfileItem item)
    {
        throw new NotImplementedException("Use UpdateUserAsync with a role parameter instead.");
    }

    public Task<ServiceActionResult<UserProfileItem>> DeleteAsync(string id)
    {
        throw new NotImplementedException("Use DeleteUserAsync instead.");
    }

    public Task<ServiceActionResult<UserProfileItem>> CreateUserAsync(UserProfileItem newUser, string role)
        => CreateUserAsync(newUser, role, null);

    public async Task<ServiceActionResult<UserProfileItem>> CreateUserAsync(
        UserProfileItem newUser, string role, string? organizationPublicId)
    {
        using var l = log.TraceScope();

        // Validate role
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new BadRequestException("Please select a role.");
        }

        await using var repo = RepositoryFactory.Create();

        // Resolve the organization BEFORE creating anything. A user with no UserOrganizations row
        // is worse than no user at all: OrganizationContextResolver returns an empty context, the
        // global query filter is fail-open and shows them every organization's data, while
        // OrganizationStampInterceptor throws on every write. Identity does not enlist in the unit
        // of work, so failing after UserManager.CreateAsync would strand the email address.
        var organizationId = await ResolveNewUserOrganizationIdAsync(repo, organizationPublicId);

        // Check if email already exists
        var existingUser = await UserManager.FindByEmailAsync(newUser.Email);
        if (existingUser != null)
        {
            throw new RecordConflictException("A user with this email address already exists.");
        }

        // Create the ApplicationUser with NO password. The user sets their own through the
        // invitation link; ConfirmEmailAsync keys the set-password step off HasPasswordAsync, so a
        // placeholder password here would silently route them to "email confirmed, now log in"
        // holding a password nobody knows.
        var user = Activator.CreateInstance<ApplicationUser>();
        user.Email = newUser.Email;
        user.UserName = newUser.Email;

        var result = await UserManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            throw new BusinessRuleException(
                $"Error creating user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        l.I($"New user created with email {newUser.Email}.");

        // Assign role. This throws rather than logging: a user with no role has no permissions and
        // no way to acquire any, and the caller was previously told the create succeeded.
        var roleResult = await UserManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            throw new BusinessRuleException(
                $"User was created but the role '{role}' could not be assigned: "
                + $"{string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
        }

        // Create UserProfile with INITIATED status
        var profilesQuery = repo.GetUserProfilesQuery(AuthorizationContext.CurrentProfile);
        var userProfile = profilesQuery.CreateNew();
        userProfile.ToRecord(newUser);
        userProfile.ApplicationUserId = user.Id;
        userProfile.Status = UserStatus.INITIATED;

        await profilesQuery.AddAsync(userProfile);

        l.I($"UserProfile created for user {newUser.Email} with INITIATED status.");

        await AssignOrganizationAsync(repo, user, organizationId);

        // Queue the invitation email
        await SendInvitationEmailAsync(user, userProfile);

        return ServiceActionResult<UserProfileItem>.OK(userProfile.ToView(user, new List<string> { role }));
    }

    /// <summary>
    /// The organization a newly created user is placed in: the one named by the caller, else the
    /// creating administrator's primary organization.
    /// </summary>
    private async Task<Guid> ResolveNewUserOrganizationIdAsync(IQueryRepository repo, string? organizationPublicId)
    {
        if (!string.IsNullOrWhiteSpace(organizationPublicId))
        {
            var organization = await repo.GetOrganizationsQuery(AuthorizationContext.CurrentProfile)
                .ByPublicIds(new[] { organizationPublicId })
                .Select()
                .FirstOrDefaultAsync();

            if (organization == null)
                throw new RecordNotFoundException($"Organization '{organizationPublicId}' not found.");

            return organization.Id;
        }

        return OperationContext.PrimaryOrganizationId
            ?? throw new BusinessRuleException(
                "Cannot create a user: no organization could be resolved for this operation. "
                + "Assign your own account to an organization first (Admin > Users > Organizations).");
    }

    /// <summary>
    /// Gives the user a single primary organization assignment scoped to itself. Mirrors what
    /// SetUserOrganizationsAsync writes, including the ApplicationUser mirror column and the
    /// resolver cache invalidation, so the assignment is live on the user's next request.
    /// </summary>
    private async Task AssignOrganizationAsync(IQueryRepository repo, ApplicationUser user, Guid organizationId)
    {
        var userOrgQuery = repo.GetUserOrganizationQuery(AuthorizationContext.CurrentProfile);
        var assignment = userOrgQuery.CreateNew();
        assignment.UserId = user.Id;
        assignment.OrganizationId = organizationId;
        assignment.Scope = OrganizationAccessScope.Self;
        assignment.IsPrimary = true;
        await userOrgQuery.AddAsync(assignment);

        user.PrimaryOrganizationId = organizationId;
        await UserManager.UpdateAsync(user);

        OrgResolver.InvalidateCache(user.Id);
    }

    private async Task<ServiceActionResult<UserProfileItem>> GetUserByIdAsync(string publicId)
    {
        using var l = log.TraceScope();

        await using var repo = RepositoryFactory.Create();
        var profile = await repo.GetUserProfilesQuery(AuthorizationContext.CurrentProfile)
            .ByPublicId(publicId)
            .Select()
            .FirstOrDefaultAsync();

        if (profile == null)
        {
            throw new RecordNotFoundException("User not found.");
        }

        var appUser = await UserManager.FindByIdAsync(profile.ApplicationUserId.ToString());
        if (appUser == null)
        {
            throw new RecordNotFoundException("User account not found.");
        }

        var roles = await UserManager.GetRolesAsync(appUser);
        return ServiceActionResult<UserProfileItem>.OK(profile.ToView(appUser, roles));
    }

    public async Task<ServiceActionResult<UserProfileItem>> UpdateUserAsync(string publicId, UserProfileItem updatedUser, string role)
    {
        using var l = log.TraceScope();

        // Validate role
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new BadRequestException("Please select a role.");
        }

        await using var repo = RepositoryFactory.Create();
        var profilesQuery = repo.GetUserProfilesQuery(AuthorizationContext.CurrentProfile);
        var profile = await profilesQuery.ByPublicId(publicId).Select().FirstOrDefaultAsync();

        if (profile == null)
        {
            throw new RecordNotFoundException("User not found.");
        }

        var appUser = await UserManager.FindByIdAsync(profile.ApplicationUserId.ToString());
        if (appUser == null)
        {
            throw new RecordNotFoundException("User account not found.");
        }

        // Check if email changed and if new email already exists
        if (!string.Equals(appUser.Email, updatedUser.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingUser = await UserManager.FindByEmailAsync(updatedUser.Email);
            if (existingUser != null)
            {
                throw new RecordConflictException("A user with this email address already exists.");
            }

            appUser.Email = updatedUser.Email;
            appUser.UserName = updatedUser.Email;
            var emailResult = await UserManager.UpdateAsync(appUser);
            if (!emailResult.Succeeded)
            {
                throw new BusinessRuleException(
                    $"Error updating email: {string.Join(", ", emailResult.Errors.Select(e => e.Description))}");
            }
        }

        // Update roles
        var currentRoles = await UserManager.GetRolesAsync(appUser);
        if (!currentRoles.Contains(role))
        {
            await UserManager.RemoveFromRolesAsync(appUser, currentRoles);
            var roleResult = await UserManager.AddToRoleAsync(appUser, role);
            if (!roleResult.Succeeded)
            {
                l.E($"Failed to assign role {role}: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            }
        }

        // Update profile — same repo as the load above so the tracked entity saves in one unit of work.
        profile.ToRecord(updatedUser);
        var updateQuery = repo.GetUserProfilesQuery(AuthorizationContext.CurrentProfile);
        await updateQuery.UpdateAsync(profile);

        l.I($"User {publicId} updated successfully.");

        var roles = await UserManager.GetRolesAsync(appUser);
        return ServiceActionResult<UserProfileItem>.OK(profile.ToView(appUser, roles));
    }

    public async Task<ServiceActionResult<bool>> DeleteUserAsync(string publicId)
    {
        using var l = log.TraceScope();

        await using var repo = RepositoryFactory.Create();
        var profilesQuery = repo.GetUserProfilesQuery(AuthorizationContext.CurrentProfile);
        var profile = await profilesQuery.ByPublicId(publicId).Select().FirstOrDefaultAsync();

        if (profile == null)
        {
            throw new RecordNotFoundException("User not found.");
        }

        var appUser = await UserManager.FindByIdAsync(profile.ApplicationUserId.ToString());

        // Delete profile first
        await profilesQuery.RemoveAsync(profile);
        l.I($"UserProfile {publicId} deleted.");

        // Delete application user if exists
        if (appUser != null)
        {
            var result = await UserManager.DeleteAsync(appUser);
            if (!result.Succeeded)
            {
                l.E($"Failed to delete ApplicationUser: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
            else
            {
                l.I($"ApplicationUser for {publicId} deleted.");
            }
        }

        return ServiceActionResult<bool>.OK(true);
    }

    /// <summary>
    /// Reports whether the user can sign in, and hands back the invitation link so an
    /// administrator can deliver it out of band when email is not configured.
    /// </summary>
    public async Task<ServiceActionResult<UserAccessStateItem>> GetUserAccessStateAsync(string userId)
    {
        using var l = log.TraceScope();

        await using var repo = RepositoryFactory.Create();
        var (_, appUser) = await ResolveUserAsync(repo, userId);

        var state = new UserAccessStateItem
        {
            EmailConfirmed = await UserManager.IsEmailConfirmedAsync(appUser),
            HasPassword = await UserManager.HasPasswordAsync(appUser)
        };

        // Only offer a link while it is still the way in. Once the account is usable, a fresh
        // confirm-email token is noise at best.
        if (!state.CanSignIn)
        {
            state.InvitationLink = await BuildInvitationLinkAsync(appUser);
        }

        return ServiceActionResult<UserAccessStateItem>.OK(state);
    }

    /// <summary>
    /// Re-queues the invitation email. The link is regenerated rather than stored — Identity
    /// tokens are derived, and every previously issued one stays valid until the security stamp
    /// changes, so a resend does not invalidate a link the user may already be holding.
    /// </summary>
    public async Task<ServiceActionResult<bool>> ResendInvitationAsync(string userId)
    {
        using var l = log.TraceScope();

        await using var repo = RepositoryFactory.Create();
        var (profile, appUser) = await ResolveUserAsync(repo, userId);

        if (await UserManager.IsEmailConfirmedAsync(appUser) && await UserManager.HasPasswordAsync(appUser))
        {
            throw new BusinessRuleException(
                "This account is already active. Use the password reset flow instead of an invitation.");
        }

        var queued = await SendInvitationEmailAsync(appUser, profile);
        if (!queued)
        {
            throw new BusinessRuleException(
                $"The invitation could not be addressed because the public site URL is unknown. "
                + $"Configure '{AccountLinkBuilder.BaseUrlKey}' in appsettings, or copy the invitation link and send it manually.");
        }

        return ServiceActionResult<bool>.OK(true);
    }

    /// <summary>
    /// Administrator-set password — the escape hatch for a deployment with no working outbound
    /// email.
    /// <para>
    /// This also confirms the email address, deliberately. <c>SignIn.RequireConfirmedAccount</c> is
    /// on, so setting a password alone still leaves the user unable to log in, and an administrator
    /// who has just handed over a password by some trusted channel has vouched for the address as
    /// firmly as a confirmation mail would.
    /// </para>
    /// </summary>
    public async Task<ServiceActionResult<bool>> SetUserPasswordAsync(string userId, string password)
    {
        using var l = log.TraceScope();

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new BadRequestException("Please provide a password.");
        }

        await using var repo = RepositoryFactory.Create();
        var (profile, appUser) = await ResolveUserAsync(repo, userId);

        // Go through a reset token rather than AddPasswordAsync: this has to work both for an
        // invited user who has no password and for an existing one who has forgotten theirs.
        var resetToken = await UserManager.GeneratePasswordResetTokenAsync(appUser);
        var result = await UserManager.ResetPasswordAsync(appUser, resetToken, password);

        if (!result.Succeeded)
        {
            throw new BusinessRuleException(
                $"Could not set the password: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        if (!await UserManager.IsEmailConfirmedAsync(appUser))
        {
            var confirmToken = await UserManager.GenerateEmailConfirmationTokenAsync(appUser);
            var confirmResult = await UserManager.ConfirmEmailAsync(appUser, confirmToken);

            if (!confirmResult.Succeeded)
            {
                throw new BusinessRuleException(
                    $"The password was set but the email could not be confirmed, so the user still "
                    + $"cannot sign in: {string.Join(", ", confirmResult.Errors.Select(e => e.Description))}");
            }
        }

        // Both halves are satisfied now — the password above, the confirmation just before — so the
        // account is usable and the profile should say so.
        if (UserActivation.ShouldActivate(profile.Status, true, true))
        {
            profile.Status = UserStatus.LIVE;
            await repo.GetUserProfilesQuery(AuthorizationContext.CurrentProfile).UpdateAsync(profile);
        }

        l.I($"Password set by administrator for user {userId}.");

        return ServiceActionResult<bool>.OK(true);
    }

    /// <summary>
    /// The absolute URL that confirms the address and offers the set-password form. Null when the
    /// public origin cannot be resolved (background work with no <c>App:BaseUrl</c> configured).
    /// </summary>
    private async Task<string?> BuildInvitationLinkAsync(ApplicationUser user)
    {
        var token = await UserManager.GenerateEmailConfirmationTokenAsync(user);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        return LinkBuilder.BuildInvitationLink(user.Id, code);
    }

    /// <summary>
    /// Queues the invitation email. Returns false, without queueing, when no absolute link can be
    /// built — an email whose only call to action is a broken link is worse than none, and the
    /// administrator has the copyable link on the Edit User page either way.
    /// </summary>
    private async Task<bool> SendInvitationEmailAsync(ApplicationUser user, UserProfile userProfile)
    {
        using var l = log.TraceScope();

        var link = await BuildInvitationLinkAsync(user);
        if (link == null)
        {
            l.W($"Invitation email for {userProfile.Email} was not queued: the public site URL could not "
                + $"be resolved. Configure '{AccountLinkBuilder.BaseUrlKey}' or send the link manually.");
            return false;
        }

        var result = await EmailTemplateService.RenderAsync(EmailTemplateName.Registration, new Dictionary<string, string>
        {
            ["FirstName"] = userProfile.FirstName,
            ["Link"] = link
        });

        // TODO: We should not instantiate EmailRequest here directly, but use a factory or builder pattern
        // We should inroduce a new interface IDevCoreEmailSender and implement it in IdentityEmailSender along with IEmailSender<ApplicationUser>
        var emailRequest = new EmailRequest
        {
            From = new EmailAddress
            {
                Address = Configuration["EmailConfiguration:FromEmail"]
                    ?? Configuration["EmailConfiguration:Username"]
                    ?? "noreply@example.com",
                Name = Configuration["EmailConfiguration:FromName"] ?? "DevCoreApp"
            },
            To = new List<EmailAddress>
            {
                new EmailAddress { Address = userProfile.Email, Name = $"{userProfile.FirstName} {userProfile.LastName}" }
            },
            Subject = result.Subject,
            IsHtml = result.IsHtml,
            Content = result.Content,
            TemplateName = EmailTemplateName.Registration
        };

        BackgroundWorker.Submit(new BackgroundRequestItem
        {
            RequestType = BackgroundRequestType.SendEmail,
            Content = emailRequest,
            OrganizationId = OperationContext.PrimaryOrganizationId
        });

        l.I($"Invitation email queued for {userProfile.Email}");
        return true;
    }

    // Read-only resolve helper. Takes the caller's repo so it shares the caller's unit of work
    // (the caller may go on to write in the same repo). Never opens its own.
    private async Task<(UserProfile Profile, ApplicationUser AppUser)> ResolveUserAsync(IQueryRepository repo, string publicId)
    {
        var profile = await repo.GetUserProfilesQuery(AuthorizationContext.CurrentProfile)
            .ByPublicId(publicId)
            .Select()
            .FirstOrDefaultAsync();

        if (profile == null)
            throw new RecordNotFoundException("User not found.");

        var appUser = await UserManager.FindByIdAsync(profile.ApplicationUserId.ToString());
        if (appUser == null)
            throw new RecordNotFoundException("User account not found.");

        return (profile, appUser);
    }

    public async Task<ServiceActionResult<List<UserOrganizationItem>>> GetUserOrganizationsAsync(string userId)
    {
        using var l = log.TraceScope();

        await using var repo = RepositoryFactory.Create();
        var (_, appUser) = await ResolveUserAsync(repo, userId);

        var userOrgs = await repo.GetUserOrganizationQuery(AuthorizationContext.CurrentProfile)
            .ByUserId(appUser.Id)
            .IncludeOrganization()
            .Select()
            .ToListAsync();

        var items = userOrgs.Select(uo => new UserOrganizationItem
        {
            OrganizationId = uo.Organization!.PublicId,
            OrganizationName = uo.Organization.Name,
            OrganizationPath = uo.Organization.Path,
            Scope = uo.Scope,
            IsPrimary = uo.IsPrimary
        }).ToList();

        return ServiceActionResult<List<UserOrganizationItem>>.OK(items);
    }

    public async Task<ServiceActionResult<bool>> SetUserOrganizationsAsync(string userId, List<UserOrganizationItem> organizations)
    {
        using var l = log.TraceScope();

        await using var repo = RepositoryFactory.Create();
        var (_, appUser) = await ResolveUserAsync(repo, userId);

        // Validate exactly one primary
        var primaryCount = organizations.Count(o => o.IsPrimary);
        if (organizations.Count > 0 && primaryCount != 1)
            throw new BusinessRuleException("Exactly one organization must be marked as primary.");

        // Resolve + validate the new assignments before touching the database.
        var userOrgQuery = repo.GetUserOrganizationQuery(AuthorizationContext.CurrentProfile);
        var newAssignments = new List<UserOrganization>();

        if (organizations.Count > 0)
        {
            // Resolve org PublicId → Guid
            var orgPublicIds = organizations.Select(o => o.OrganizationId).ToList();
            var orgLookup = await repo.GetOrganizationsQuery(AuthorizationContext.CurrentProfile)
                .ByPublicIds(orgPublicIds)
                .Select()
                .ToDictionaryAsync(o => o.PublicId, o => o.Id);

            foreach (var item in organizations)
            {
                if (!orgLookup.TryGetValue(item.OrganizationId, out var orgId))
                    throw new RecordNotFoundException($"Organization '{item.OrganizationId}' not found.");

                var assignment = userOrgQuery.CreateNew();
                assignment.UserId = appUser.Id;
                assignment.OrganizationId = orgId;
                assignment.Scope = item.Scope;
                assignment.IsPrimary = item.IsPrimary;
                newAssignments.Add(assignment);
            }

            // Update primary organization on ApplicationUser
            var primaryOrg = organizations.First(o => o.IsPrimary);
            appUser.PrimaryOrganizationId = orgLookup[primaryOrg.OrganizationId];
        }
        else
        {
            appUser.PrimaryOrganizationId = null;
        }

        await userOrgQuery.ReplaceForUserAsync(appUser.Id, newAssignments);
        await UserManager.UpdateAsync(appUser);

        OrgResolver.InvalidateCache(appUser.Id);

        l.I($"Organization assignments updated for user {userId}.");

        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<List<PermissionOverrideItem>>> GetUserPermissionOverridesAsync(string userId)
    {
        using var l = log.TraceScope();

        await using var repo = RepositoryFactory.Create();
        var (_, appUser) = await ResolveUserAsync(repo, userId);

        var overrides = await repo.GetUserPermissionOverrideQuery(AuthorizationContext.CurrentProfile)
            .ByUserId(appUser.Id)
            .IncludePermission()
            .Select()
            .ToListAsync();

        var items = overrides.Select(upo => new PermissionOverrideItem
        {
            PermissionKey = upo.Permission!.Key,
            IsGranted = upo.IsGranted,
            Reason = upo.Reason
        }).ToList();

        return ServiceActionResult<List<PermissionOverrideItem>>.OK(items);
    }

    public async Task<ServiceActionResult<bool>> SetUserPermissionOverridesAsync(string userId, List<PermissionOverrideItem> overrides)
    {
        using var l = log.TraceScope();

        await using var repo = RepositoryFactory.Create();
        var (_, appUser) = await ResolveUserAsync(repo, userId);

        var overrideQuery = repo.GetUserPermissionOverrideQuery(AuthorizationContext.CurrentProfile);
        var newOverrides = new List<UserPermissionOverride>();

        if (overrides.Count > 0)
        {
            var permissionKeys = overrides.Select(o => o.PermissionKey).ToList();
            var permLookup = await repo.GetPermissionQuery(AuthorizationContext.CurrentProfile)
                .ByKeys(permissionKeys)
                .Select()
                .ToDictionaryAsync(p => p.Key, p => p.Id);

            foreach (var item in overrides)
            {
                if (!permLookup.TryGetValue(item.PermissionKey, out var permId))
                    continue;

                var record = overrideQuery.CreateNew();
                record.UserId = appUser.Id;
                record.PermissionId = permId;
                record.IsGranted = item.IsGranted;
                record.Reason = item.Reason;
                newOverrides.Add(record);
            }
        }

        await overrideQuery.ReplaceForUserAsync(appUser.Id, newOverrides);

        l.I($"Permission overrides updated for user {userId}.");

        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<List<EffectivePermissionItem>>> GetEffectivePermissionsAsync(string userId)
    {
        using var l = log.TraceScope();

        await using var repo = RepositoryFactory.Create();
        var (_, appUser) = await ResolveUserAsync(repo, userId);

        // Load all permissions
        var allPermissions = await repo.GetPermissionQuery(AuthorizationContext.CurrentProfile)
            .OrderedByDisplayOrder()
            .Select()
            .ToListAsync();

        // Get user roles
        var roles = await UserManager.GetRolesAsync(appUser);

        var rolePermissionQuery = repo.GetRolePermissionQuery(AuthorizationContext.CurrentProfile);

        // Get role IDs
        var roleIds = await rolePermissionQuery.GetRoleIdsByNamesAsync(roles.ToList());

        // Get role→permission mappings (track which role grants each permission)
        var rolePermissions = await rolePermissionQuery.GetRolePermissionGrantsForRoleIdsAsync(roleIds);

        var roleGrantsByPermId = rolePermissions
            .GroupBy(x => x.PermissionId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName ?? "Unknown").ToList());

        // Get user overrides
        var overrides = (await repo.GetUserPermissionOverrideQuery(AuthorizationContext.CurrentProfile)
            .ByUserId(appUser.Id)
            .Select()
            .ToListAsync())
            .ToDictionary(upo => upo.PermissionId, upo => upo);

        var items = allPermissions.Select(p =>
        {
            var roleGrants = roleGrantsByPermId.GetValueOrDefault(p.Id);
            var hasOverride = overrides.TryGetValue(p.Id, out var userOverride);

            bool isGranted;
            string source;

            if (hasOverride && userOverride!.IsGranted)
            {
                isGranted = true;
                source = roleGrants != null
                    ? $"Override: Granted (also via Role: {string.Join(", ", roleGrants)})"
                    : "Override: Granted";
            }
            else if (hasOverride && !userOverride!.IsGranted)
            {
                isGranted = false;
                source = roleGrants != null
                    ? $"Override: Denied (overrides Role: {string.Join(", ", roleGrants)})"
                    : "Override: Denied";
            }
            else if (roleGrants != null)
            {
                isGranted = true;
                source = $"Role: {string.Join(", ", roleGrants)}";
            }
            else
            {
                isGranted = false;
                source = "Not granted";
            }

            return new EffectivePermissionItem
            {
                Key = p.Key,
                Module = p.Module,
                Entity = p.Entity,
                Action = p.Action,
                IsGranted = isGranted,
                Source = source
            };
        }).ToList();

        return ServiceActionResult<List<EffectivePermissionItem>>.OK(items);
    }

    public async Task<ServiceActionResult<UserProfileItem>> UploadProfilePictureAsync(string userId, Stream imageStream, string contentType)
    {
        using var l = log.TraceScope();

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            throw new BadRequestException("Only JPEG, PNG, and WebP images are allowed.");

        using var memStream = new MemoryStream();
        await imageStream.CopyToAsync(memStream);

        if (memStream.Length > 2 * 1024 * 1024)
            throw new BadRequestException("Image must be less than 2 MB.");

        var imageData = memStream.ToArray();
        var picture = ResizeImage(imageData, 400, 400);
        var thumbnail = ResizeImage(imageData, 48, 48);

        await using var repo = RepositoryFactory.Create();
        var profilesQuery = repo.GetUserProfilesQuery(AuthorizationContext.CurrentProfile);
        var profile = await profilesQuery.ByPublicId(userId).Select().FirstOrDefaultAsync();
        if (profile == null)
            throw new RecordNotFoundException("User not found.");

        profile.ProfilePicture = picture;
        profile.ProfilePictureContentType = "image/jpeg";
        profile.ProfilePictureThumbnail = thumbnail;
        await profilesQuery.UpdateAsync(profile);

        l.I($"Profile picture uploaded for user {userId}.");

        var appUser = await UserManager.FindByIdAsync(profile.ApplicationUserId.ToString());
        var roles = appUser != null ? await UserManager.GetRolesAsync(appUser) : null;
        return ServiceActionResult<UserProfileItem>.OK(profile.ToView(appUser, roles));
    }

    public async Task<ServiceActionResult<bool>> DeleteProfilePictureAsync(string userId)
    {
        using var l = log.TraceScope();

        await using var repo = RepositoryFactory.Create();
        var profilesQuery = repo.GetUserProfilesQuery(AuthorizationContext.CurrentProfile);
        var profile = await profilesQuery.ByPublicId(userId).Select().FirstOrDefaultAsync();
        if (profile == null)
            throw new RecordNotFoundException("User not found.");

        profile.ProfilePicture = null;
        profile.ProfilePictureContentType = null;
        profile.ProfilePictureThumbnail = null;
        await profilesQuery.UpdateAsync(profile);

        l.I($"Profile picture deleted for user {userId}.");

        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<(byte[] Data, string ContentType)>> GetProfilePictureAsync(string userId)
    {
        using var l = log.TraceScope();

        await using var repo = RepositoryFactory.Create();
        var profile = await repo.GetUserProfilesQuery(AuthorizationContext.CurrentProfile)
            .ByPublicId(userId).Select().FirstOrDefaultAsync();

        if (profile == null)
            throw new RecordNotFoundException("User not found.");

        if (profile.ProfilePicture == null || string.IsNullOrEmpty(profile.ProfilePictureContentType))
            throw new RecordNotFoundException("No profile picture found.");

        return ServiceActionResult<(byte[] Data, string ContentType)>.OK(
            (profile.ProfilePicture, profile.ProfilePictureContentType));
    }

    public async Task<ServiceActionResult<(byte[] Data, string ContentType)>> GetProfilePictureThumbnailAsync(string userId)
    {
        using var l = log.TraceScope();

        await using var repo = RepositoryFactory.Create();
        var profile = await repo.GetUserProfilesQuery(AuthorizationContext.CurrentProfile)
            .ByPublicId(userId).Select().FirstOrDefaultAsync();

        if (profile == null)
            throw new RecordNotFoundException("User not found.");

        if (profile.ProfilePictureThumbnail == null || string.IsNullOrEmpty(profile.ProfilePictureContentType))
            throw new RecordNotFoundException("No profile picture found.");

        return ServiceActionResult<(byte[] Data, string ContentType)>.OK(
            (profile.ProfilePictureThumbnail, profile.ProfilePictureContentType));
    }

    /// <summary>
    /// JPEG at quality 85 — the format ProfilePictureContentType is set to on upload. Scaling
    /// itself lives in the shared <see cref="ImageResizer"/>; this wrapper only turns an
    /// undecodable upload back into the BadRequestException callers already expect.
    /// </summary>
    private static byte[] ResizeImage(byte[] imageData, int maxWidth, int maxHeight)
        => ImageResizer.Resize(imageData, maxWidth, maxHeight, SKEncodedImageFormat.Jpeg, 85)
           ?? throw new BadRequestException("Invalid image data.");
}
