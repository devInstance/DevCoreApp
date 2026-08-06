using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Admin.Services.Background;
using DevInstance.DevCoreApp.Server.Admin.Services.Background.Requests;
using DevInstance.DevCoreApp.Server.Admin.Services.Exceptions;
using DevInstance.DevCoreApp.Server.Admin.Services.Notifications.Templates;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.EmailProcessor;
using DevInstance.DevCoreApp.Shared.Model.Core;
using DevInstance.DevCoreApp.Shared.Model.Core.UserAdmin;
using DevInstance.DevCoreApp.Shared.Utils.Core;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;
using DevInstance.WebServiceToolkit.Database.Queries.Extensions;
using DevInstance.WebServiceToolkit.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Text;
using SkiaSharp;

namespace DevInstance.DevCoreApp.Server.Admin.Services.UserAdmin;

[BlazorService]
public class UserProfileService : BaseService, IUserProfileService
{
    public UserManager<ApplicationUser> UserManager { get; }
    private IUserStore<ApplicationUser> UserStore { get; }
    private IBackgroundWorker BackgroundWorker { get; }
    private IEmailTemplateService EmailTemplateService { get; }
    private IOrganizationContextResolver OrgResolver { get; }
    private IHttpContextAccessor HttpContextAccessor { get; }
    private IOperationContext OperationContext { get; }

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
                              IHttpContextAccessor httpContextAccessor,
                              IOperationContext operationContext)
        : base(logManager, timeProvider, repositoryFactory, authorizationContext)
    {
        log = logManager.CreateLogger(this);

        UserManager = userManager;
        UserStore = userStore;
        BackgroundWorker = backgroundWorker;
        EmailTemplateService = emailTemplateService;
        OrgResolver = orgResolver;
        HttpContextAccessor = httpContextAccessor;
        OperationContext = operationContext;
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

        var modelList = ModelListResult.CreateList(users.ToArray(), totalCount, top, page, sortBy, search, true);
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

    public async Task<ServiceActionResult<UserProfileItem>> CreateUserAsync(UserProfileItem newUser, string role)
    {
        using var l = log.TraceScope();

        // Validate role
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new BadRequestException("Please select a role.");
        }

        // Check if email already exists
        var existingUser = await UserManager.FindByEmailAsync(newUser.Email);
        if (existingUser != null)
        {
            throw new RecordConflictException("A user with this email address already exists.");
        }

        // Create ApplicationUser without a password. The invited user will set it after confirming email.
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

        // Assign role
        var roleResult = await UserManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            l.E($"Failed to assign role {role}: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
        }

        // Create UserProfile with INITIATED status
        await using var repo = RepositoryFactory.Create();
        var profilesQuery = repo.GetUserProfilesQuery(AuthorizationContext.CurrentProfile);
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

    private async Task SendRegistrationEmailAsync(ApplicationUser user, UserProfile userProfile)
    {
        using var l = log.TraceScope();

        var token = await UserManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = BuildRegistrationConfirmationLink(user.Id.ToString(), encodedToken);

        var result = await EmailTemplateService.RenderAsync(EmailTemplateName.Registration, new Dictionary<string, string>
        {
            ["FirstName"] = userProfile.FirstName,
            ["Link"] = link
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
            Content = emailRequest,
            OrganizationId = OperationContext.PrimaryOrganizationId
        });

        l.I($"Registration email queued for {userProfile.Email}");
    }

    private string BuildRegistrationConfirmationLink(string userId, string code)
    {
        var httpContext = HttpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("Cannot build registration confirmation link without an active HTTP request.");

        var baseUri = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
        return $"{baseUri}/account/confirm-email?userId={Uri.EscapeDataString(userId)}&code={Uri.EscapeDataString(code)}";
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

    private static byte[] ResizeImage(byte[] imageData, int maxWidth, int maxHeight)
    {
        using var original = SKBitmap.Decode(imageData);
        if (original == null)
            throw new BadRequestException("Invalid image data.");

        var ratioX = (double)maxWidth / original.Width;
        var ratioY = (double)maxHeight / original.Height;
        var ratio = Math.Min(ratioX, ratioY);
        ratio = Math.Min(ratio, 1.0); // Don't upscale

        var newWidth = (int)(original.Width * ratio);
        var newHeight = (int)(original.Height * ratio);

        using var resized = original.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);

        return data.ToArray();
    }
}
