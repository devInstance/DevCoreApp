using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.EmailProcessor;
using DevInstance.DevCoreApp.Server.WebService.Authentication;
using DevInstance.DevCoreApp.Server.WebService.Background;
using DevInstance.DevCoreApp.Server.WebService.Background.Requests;
using DevInstance.DevCoreApp.Server.WebService.Notifications.Templates;
using DevInstance.DevCoreApp.Server.WebService.Tools;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;
using DevInstance.WebServiceToolkit.Database.Queries.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DevInstance.DevCoreApp.Server.WebService.Services;

[AppService]
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
