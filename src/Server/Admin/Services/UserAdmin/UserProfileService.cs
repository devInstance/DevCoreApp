using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Admin.Services.Background;
using DevInstance.DevCoreApp.Server.Admin.Services.Background.Requests;
using DevInstance.DevCoreApp.Server.Admin.Services.Exceptions;
using DevInstance.DevCoreApp.Server.Admin.Services.Notifications.Templates;
using DevInstance.DevCoreApp.Server.Database.Core;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Server.EmailProcessor;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.DevCoreApp.Shared.Model.UserAdmin;
using DevInstance.DevCoreApp.Shared.Utils;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;
using DevInstance.WebServiceToolkit.Database.Queries.Extensions;
using DevInstance.WebServiceToolkit.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DevInstance.DevCoreApp.Server.Admin.Services.UserAdmin;

[BlazorService]
public class UserProfileService : BaseService, IUserProfileService
{
    public UserManager<ApplicationUser> UserManager { get; }
    private IUserStore<ApplicationUser> UserStore { get; }
    private IBackgroundWorker BackgroundWorker { get; }
    private IEmailTemplateService EmailTemplateService { get; }
    private ApplicationDbContext Db { get; }
    private IOrganizationContextResolver OrgResolver { get; }

    private IScopeLog log;

    public UserProfileService(IScopeManager logManager,
                              ITimeProvider timeProvider,
                              IQueryRepository query,
                              IAuthorizationContext authorizationContext,
                              UserManager<ApplicationUser> userManager,
                              IUserStore<ApplicationUser> userStore,
                              IBackgroundWorker backgroundWorker,
                              IEmailTemplateService emailTemplateService,
                              ApplicationDbContext db,
                              IOrganizationContextResolver orgResolver)
        : base(logManager, timeProvider, query, authorizationContext)
    {
        log = logManager.CreateLogger(this);

        UserManager = userManager;
        UserStore = userStore;
        BackgroundWorker = backgroundWorker;
        EmailTemplateService = emailTemplateService;
        Db = db;
        OrgResolver = orgResolver;
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

    public async Task<ServiceActionResult<ModelList<UserProfileItem>>> GetListAsync(int? top, int? page, string[] sortBy = null, string search = null)
    {
        using var l = log.TraceScope();

        var profilesQuery = Repository.GetUserProfilesQuery(AuthorizationContext.CurrentProfile);

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

        // Create ApplicationUser with a temporary random password
        var user = Activator.CreateInstance<ApplicationUser>();
        user.Email = newUser.Email;
        user.UserName = newUser.Email;

        var tempPassword = IdGenerator.New();

        var result = await UserManager.CreateAsync(user, tempPassword);
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

    private async Task<ServiceActionResult<UserProfileItem>> GetUserByIdAsync(string publicId)
    {
        using var l = log.TraceScope();

        var profile = await Repository.GetUserProfilesQuery(AuthorizationContext.CurrentProfile)
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

        var profilesQuery = Repository.GetUserProfilesQuery(AuthorizationContext.CurrentProfile);
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

    private async Task<(UserProfile Profile, ApplicationUser AppUser)> ResolveUserAsync(string publicId)
    {
        var profile = await Repository.GetUserProfilesQuery(AuthorizationContext.CurrentProfile)
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

        var (_, appUser) = await ResolveUserAsync(userId);

        var userOrgs = await Db.UserOrganizations
            .Include(uo => uo.Organization)
            .Where(uo => uo.UserId == appUser.Id)
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

        var (_, appUser) = await ResolveUserAsync(userId);

        // Validate exactly one primary
        var primaryCount = organizations.Count(o => o.IsPrimary);
        if (organizations.Count > 0 && primaryCount != 1)
            throw new BusinessRuleException("Exactly one organization must be marked as primary.");

        // Remove existing assignments
        var existing = await Db.UserOrganizations
            .Where(uo => uo.UserId == appUser.Id)
            .ToListAsync();
        Db.UserOrganizations.RemoveRange(existing);

        if (organizations.Count > 0)
        {
            // Resolve org PublicId → Guid
            var orgPublicIds = organizations.Select(o => o.OrganizationId).ToList();
            var orgLookup = await Db.Organizations
                .Where(o => orgPublicIds.Contains(o.PublicId))
                .ToDictionaryAsync(o => o.PublicId, o => o.Id);

            foreach (var item in organizations)
            {
                if (!orgLookup.TryGetValue(item.OrganizationId, out var orgId))
                    throw new RecordNotFoundException($"Organization '{item.OrganizationId}' not found.");

                Db.UserOrganizations.Add(new UserOrganization
                {
                    Id = Guid.NewGuid(),
                    UserId = appUser.Id,
                    OrganizationId = orgId,
                    Scope = item.Scope,
                    IsPrimary = item.IsPrimary
                });
            }

            // Update primary organization on ApplicationUser
            var primaryOrg = organizations.First(o => o.IsPrimary);
            appUser.PrimaryOrganizationId = orgLookup[primaryOrg.OrganizationId];
        }
        else
        {
            appUser.PrimaryOrganizationId = null;
        }

        await UserManager.UpdateAsync(appUser);
        await Db.SaveChangesAsync();

        OrgResolver.InvalidateCache(appUser.Id);

        l.I($"Organization assignments updated for user {userId}.");

        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<List<PermissionOverrideItem>>> GetUserPermissionOverridesAsync(string userId)
    {
        using var l = log.TraceScope();

        var (_, appUser) = await ResolveUserAsync(userId);

        var overrides = await Db.UserPermissionOverrides
            .Include(upo => upo.Permission)
            .Where(upo => upo.UserId == appUser.Id)
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

        var (_, appUser) = await ResolveUserAsync(userId);

        // Remove existing overrides
        var existing = await Db.UserPermissionOverrides
            .Where(upo => upo.UserId == appUser.Id)
            .ToListAsync();
        Db.UserPermissionOverrides.RemoveRange(existing);

        if (overrides.Count > 0)
        {
            var permissionKeys = overrides.Select(o => o.PermissionKey).ToList();
            var permLookup = await Db.Permissions
                .Where(p => permissionKeys.Contains(p.Key))
                .ToDictionaryAsync(p => p.Key, p => p.Id);

            foreach (var item in overrides)
            {
                if (!permLookup.TryGetValue(item.PermissionKey, out var permId))
                    continue;

                Db.UserPermissionOverrides.Add(new UserPermissionOverride
                {
                    Id = Guid.NewGuid(),
                    UserId = appUser.Id,
                    PermissionId = permId,
                    IsGranted = item.IsGranted,
                    Reason = item.Reason
                });
            }
        }

        await Db.SaveChangesAsync();

        l.I($"Permission overrides updated for user {userId}.");

        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<List<EffectivePermissionItem>>> GetEffectivePermissionsAsync(string userId)
    {
        using var l = log.TraceScope();

        var (_, appUser) = await ResolveUserAsync(userId);

        // Load all permissions
        var allPermissions = await Db.Permissions
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();

        // Get user roles
        var roles = await UserManager.GetRolesAsync(appUser);

        // Get role IDs
        var roleIds = await Db.Roles
            .Where(r => roles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync();

        // Get role→permission mappings (track which role grants each permission)
        var rolePermissions = await Db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Join(Db.Roles, rp => rp.RoleId, r => r.Id, (rp, r) => new { rp.PermissionId, RoleName = r.Name })
            .ToListAsync();

        var roleGrantsByPermId = rolePermissions
            .GroupBy(x => x.PermissionId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName ?? "Unknown").ToList());

        // Get user overrides
        var overrides = await Db.UserPermissionOverrides
            .Where(upo => upo.UserId == appUser.Id)
            .ToDictionaryAsync(upo => upo.PermissionId, upo => upo);

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
}
