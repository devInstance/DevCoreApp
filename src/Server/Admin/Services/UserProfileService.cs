using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Admin.Services.Background;
using DevInstance.DevCoreApp.Server.Admin.Services.Background.Requests;
using DevInstance.DevCoreApp.Server.Admin.Services.Notifications.Templates;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.EmailProcessor;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;
using DevInstance.WebServiceToolkit.Database.Queries.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DevInstance.DevCoreApp.Server.Admin.Services;

[BlazorService]
public class UserProfileService : BaseService
{
    public UserManager<ApplicationUser> UserManager { get; }
    private IUserStore<ApplicationUser> UserStore { get; }
    private IBackgroundWorker BackgroundWorker { get; }
    private IEmailTemplateService EmailTemplateService { get; }

    private IScopeLog log;

    public UserProfileService(IScopeManager logManager,
                              ITimeProvider timeProvider,
                              IQueryRepository query,
                              IAuthorizationContext authorizationContext,
                              UserManager<ApplicationUser> userManager,
                              IUserStore<ApplicationUser> userStore,
                              IBackgroundWorker backgroundWorker,
                              IEmailTemplateService emailTemplateService)
        : base(logManager, timeProvider, query, authorizationContext)
    {
        log = logManager.CreateLogger(this);

        UserManager = userManager;
        UserStore = userStore;
        BackgroundWorker = backgroundWorker;
        EmailTemplateService = emailTemplateService;
    }

    public ServiceActionResult<UserProfileItem> GetCurrentUser()
    {
        return ServiceActionResult<UserProfileItem>.OK(AuthorizationContext.CurrentProfile.ToView());
    }

    public async Task<ServiceActionResult<UserProfileItem>> UpdateCurrentUserAsync(UserProfileItem newProfile)
    {
        var profile = AuthorizationContext.CurrentProfile;
        profile.ToRecord(newProfile);
        await Repository.GetUserProfilesQuery(AuthorizationContext.CurrentProfile).UpdateAsync(profile);

        return ServiceActionResult<UserProfileItem>.OK(profile.ToView());
    }

    public async Task<ServiceActionResult<ModelList<UserProfileItem>>> GetAllUsersAsync(int? top, int? page, string? sortField = null, bool? isAsc = null, string? search = null)
    {
        using var l = log.TraceScope();

        var profilesQuery = Repository.GetUserProfilesQuery(AuthorizationContext.CurrentProfile);

        if (!string.IsNullOrEmpty(search))
        {
            profilesQuery = profilesQuery.Search(search);
        }

        if (!string.IsNullOrEmpty(sortField))
        {
            profilesQuery = profilesQuery.SortBy(sortField, isAsc ?? true);
        }

        var totalCount = await profilesQuery.Clone().Select().CountAsync();
        var userProfiles = await profilesQuery.Paginate(top, page).Select().ToListAsync();

        var users = new List<UserProfileItem>();

        foreach (var profile in userProfiles)
        {
            var appUser = await UserManager.FindByIdAsync(profile.ApplicationUserId.ToString());

            if (appUser != null)
            {
                var roles = await UserManager.GetRolesAsync(appUser);
                var newUserViewModel = profile.ToView(appUser, roles);

                users.Add(newUserViewModel);
            }
        }

        var modelList = ModelListResult.CreateList(users.ToArray(), totalCount, top, page, sortField, isAsc, search);
        return ServiceActionResult<ModelList<UserProfileItem>>.OK(modelList);
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

    public async Task<ServiceActionResult<UserProfileItem>> CreateUserAsync(UserProfileItem newUser, string role)
    {
        using var l = log.TraceScope();

        // Validate role
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new InvalidOperationException("Please select a role.");
        }

        // Check if email already exists
        var existingUser = await UserManager.FindByEmailAsync(newUser.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("A user with this email address already exists.");
        }

        // Create ApplicationUser with a temporary random password
        var user = Activator.CreateInstance<ApplicationUser>();
        user.Email = newUser.Email;
        user.UserName = newUser.Email;

        var tempPassword = IdGenerator.New();

        var result = await UserManager.CreateAsync(user, tempPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Error creating user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        l.I($"New user created with email {newUser.Email}.");

        // Assign role
        var roleResult = await UserManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            l.E($"Failed to assign role {role}: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
        }

        // Create UserProfile with INITIATED status
        var profilesQuery = Repository.GetUserProfilesQuery(AuthorizationContext.CurrentProfile);
        var userProfile = profilesQuery.CreateNew();
        userProfile.ToRecord(newUser);
        userProfile.ApplicationUserId = user.Id;
        userProfile.Status = UserStatus.INITIATED;

        await profilesQuery.AddAsync(userProfile);

        l.I($"UserProfile created for user {newUser.Email} with INITIATED status.");

        // Queue registration email
        await SendRegistrationEmailAsync(user, userProfile);

        return ServiceActionResult<UserProfileItem>.OK(userProfile.ToView(user, new List<string> { role }));
    }

    public async Task<ServiceActionResult<UserProfileItem>> GetUserByIdAsync(string publicId)
    {
        using var l = log.TraceScope();

        var profile = await Repository.GetUserProfilesQuery(AuthorizationContext.CurrentProfile)
            .ByPublicId(publicId)
            .Select()
            .FirstOrDefaultAsync();

        if (profile == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        var appUser = await UserManager.FindByIdAsync(profile.ApplicationUserId.ToString());
        if (appUser == null)
        {
            throw new InvalidOperationException("User account not found.");
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
            throw new InvalidOperationException("Please select a role.");
        }

        var profilesQuery = Repository.GetUserProfilesQuery(AuthorizationContext.CurrentProfile);
        var profile = await profilesQuery.ByPublicId(publicId).Select().FirstOrDefaultAsync();

        if (profile == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        var appUser = await UserManager.FindByIdAsync(profile.ApplicationUserId.ToString());
        if (appUser == null)
        {
            throw new InvalidOperationException("User account not found.");
        }

        // Check if email changed and if new email already exists
        if (!string.Equals(appUser.Email, updatedUser.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingUser = await UserManager.FindByEmailAsync(updatedUser.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("A user with this email address already exists.");
            }

            appUser.Email = updatedUser.Email;
            appUser.UserName = updatedUser.Email;
            var emailResult = await UserManager.UpdateAsync(appUser);
            if (!emailResult.Succeeded)
            {
                throw new InvalidOperationException(
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

        // Update profile
        profile.ToRecord(updatedUser);
        var updateQuery = Repository.GetUserProfilesQuery(AuthorizationContext.CurrentProfile);
        await updateQuery.UpdateAsync(profile);

        l.I($"User {publicId} updated successfully.");

        var roles = await UserManager.GetRolesAsync(appUser);
        return ServiceActionResult<UserProfileItem>.OK(profile.ToView(appUser, roles));
    }

    public async Task<ServiceActionResult<bool>> DeleteUserAsync(string publicId)
    {
        using var l = log.TraceScope();

        var profilesQuery = Repository.GetUserProfilesQuery(AuthorizationContext.CurrentProfile);
        var profile = await profilesQuery.ByPublicId(publicId).Select().FirstOrDefaultAsync();

        if (profile == null)
        {
            throw new InvalidOperationException("User not found.");
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

    private async Task SendRegistrationEmailAsync(ApplicationUser user, UserProfile userProfile)
    {
        using var l = log.TraceScope();

        var token = await UserManager.GeneratePasswordResetTokenAsync(user);

        var result = await EmailTemplateService.RenderAsync(EmailTemplateName.Registration, new Dictionary<string, string>
        {
            ["FirstName"] = userProfile.FirstName,
            ["Link"] = token
        });

        // TODO: We should not instantiate EmailRequest here directly, but use a factory or builder pattern
        // We should inroduce a new interface IDevCoreEmailSender and implement it in IdentityEmailSender along with IEmailSender<ApplicationUser>
        var emailRequest = new EmailRequest
        {
            From = new EmailAddress { Address = "noreply@example.com", Name = "DevCoreApp" },
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
            Content = emailRequest
        });

        l.I($"Registration email queued for {userProfile.Email}");
    }
}
