using Bogus;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Admin.Services.UserAdmin;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.DevCoreApp.Shared.Model.Common;
using DevInstance.DevCoreApp.Shared.Model.Permissions;
using DevInstance.DevCoreApp.Shared.Model.UserAdmin;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Mocks.UserAdmin;

[BlazorServiceMock]
public class UserProfileServiceMock : IUserProfileService
{
    const int TotalCount = 100;
    List<UserProfileItem> modelList = new List<UserProfileItem>();

    private Dictionary<string, List<UserOrganizationItem>> userOrganizations = new();
    private Dictionary<string, List<PermissionOverrideItem>> userOverrides = new();

    private readonly List<(string Id, string Name, string Path)> mockOrgs = new()
    {
        ("org-root", "Acme Corp", "Acme Corp"),
        ("org-east", "East Region", "Acme Corp / East Region"),
        ("org-west", "West Region", "Acme Corp / West Region"),
        ("org-ny", "New York Office", "Acme Corp / East Region / New York Office"),
        ("org-boston", "Boston Office", "Acme Corp / East Region / Boston Office")
    };

    private int delay = 500;

    public UserProfileServiceMock()
    {
        var faker = CreateFaker();
        modelList = faker.Generate(TotalCount);

        // Seed first user with org assignment
        var firstUser = modelList.First();
        userOrganizations[firstUser.Id] = new List<UserOrganizationItem>
        {
            new() { OrganizationId = "org-root", OrganizationName = "Acme Corp", OrganizationPath = "Acme Corp", Scope = OrganizationAccessScope.WithChildren, IsPrimary = true }
        };
    }

    private static Faker<UserProfileItem> CreateFaker()
    {
        return new Faker<UserProfileItem>()
            .RuleFor(u => u.Id, f => IdGenerator.New())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.MiddleName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber("(###) ###-####"))
            .RuleFor(u => u.Roles, f => f.PickRandom(ApplicationRoles.Admin, ApplicationRoles.Manager, ApplicationRoles.Employee, ApplicationRoles.Client))
            .RuleFor(u => u.Status, f => f.PickRandom("Active", "Initiated", "Disabled"))
            .RuleFor(u => u.CreateDate, f => f.Date.Past(2))
            .RuleFor(u => u.UpdateDate, (f, u) => f.Date.Between(u.CreateDate, DateTime.UtcNow));
    }

    public ServiceActionResult<UserProfileItem> GetCurrentUser()
    {
        return ServiceActionResult<UserProfileItem>.OK(modelList.First());
    }

    public async Task<ServiceActionResult<UserProfileItem>> UpdateCurrentUserAsync(UserProfileItem newProfile)
    {
        await Task.Delay(delay);

        var item = modelList.First();
        item.FirstName = newProfile.FirstName;
        item.MiddleName = newProfile.MiddleName;
        item.LastName = newProfile.LastName;
        item.PhoneNumber = newProfile.PhoneNumber;

        return ServiceActionResult<UserProfileItem>.OK(item);
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

    public async Task<ServiceActionResult<ModelList<UserProfileItem>>> GetListAsync(int? top, int? page, string[] sortBy, string search)
    {
        var pageVal = page ?? 0;
        var topVal = top ?? 10;

        ParseSearch(search, out var term, out var field, out var status, out var days);

        var filtered = modelList.AsEnumerable();

        if (!string.IsNullOrEmpty(status))
        {
            filtered = filtered.Where(u => u.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        if (days > 0)
        {
            var cutoff = DateTime.UtcNow.AddDays(-days);
            filtered = filtered.Where(u => u.UpdateDate >= cutoff);
        }

        if (!string.IsNullOrEmpty(term))
        {
            filtered = field switch
            {
                "firstname" => filtered.Where(u => u.FirstName.Contains(term, StringComparison.OrdinalIgnoreCase)),
                "lastname" => filtered.Where(u => u.LastName.Contains(term, StringComparison.OrdinalIgnoreCase)),
                _ => filtered.Where(u => u.FirstName.Contains(term, StringComparison.OrdinalIgnoreCase)
                                      || u.LastName.Contains(term, StringComparison.OrdinalIgnoreCase)
                                      || u.Email.Contains(term, StringComparison.OrdinalIgnoreCase)
                                      || (u.PhoneNumber != null && u.PhoneNumber.Contains(term, StringComparison.OrdinalIgnoreCase)))
            };

            var searchResult = filtered
                .Take(topVal)
                .Select(u => new UserProfileItem
                {
                    Id = u.Id,
                    Email = u.Email.Replace(term, $"<mark>{term}</mark>", StringComparison.OrdinalIgnoreCase),
                    FirstName = u.FirstName.Replace(term, $"<mark>{term}</mark>", StringComparison.OrdinalIgnoreCase),
                    MiddleName = u.MiddleName,
                    LastName = u.LastName.Replace(term, $"<mark>{term}</mark>", StringComparison.OrdinalIgnoreCase),
                    PhoneNumber = u.PhoneNumber?.Replace(term, $"<mark>{term}</mark>", StringComparison.OrdinalIgnoreCase) ?? "",
                    Roles = u.Roles,
                    Status = u.Status,
                    CreateDate = u.CreateDate,
                    UpdateDate = u.UpdateDate
                })
                .ToList();

            return ServiceActionResult<ModelList<UserProfileItem>>.OK(
                ModelListResult.CreateList(searchResult.ToArray(), searchResult.Count, topVal, pageVal, sortBy, search, true));
        }

        if (days > 0 || !string.IsNullOrEmpty(status))
        {
            var filteredList = filtered.ToList();
            var dateItems = filteredList
                .Skip(pageVal * topVal)
                .Take(topVal)
                .ToArray();

            await Task.Delay(delay);

            return ServiceActionResult<ModelList<UserProfileItem>>.OK(
                ModelListResult.CreateList(dateItems, filteredList.Count, topVal, pageVal, sortBy, search, true));
        }

        var items = modelList
            .Skip(pageVal * topVal)
            .Take(topVal)
            .ToArray();

        await Task.Delay(delay);

        return ServiceActionResult<ModelList<UserProfileItem>>.OK(
            ModelListResult.CreateList(items, modelList.Count, topVal, pageVal, sortBy, search));
    }

    private static void ParseSearch(string? search, out string term, out string field, out string status, out int days)
    {
        term = string.Empty;
        field = string.Empty;
        status = string.Empty;
        days = 0;

        if (string.IsNullOrEmpty(search)) return;

        var parts = search.Split('|');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("field:", StringComparison.OrdinalIgnoreCase))
                field = trimmed[6..];
            else if (trimmed.StartsWith("status:", StringComparison.OrdinalIgnoreCase))
                status = trimmed[7..];
            else if (trimmed.StartsWith("days:", StringComparison.OrdinalIgnoreCase) && int.TryParse(trimmed[5..], out var d))
                days = d;
            else
                term = trimmed;
        }
    }

    public async Task<ServiceActionResult<UserProfileItem>> GetAsync(string id)
    {
        var item = modelList.Find(i => i.Id == id);

        await Task.Delay(delay);

        return ServiceActionResult<UserProfileItem>.OK(item!);
    }

    public async Task<ServiceActionResult<UserProfileItem>> AddAsync(UserProfileItem item)
    {
        item.Id = IdGenerator.New();
        modelList.Add(item);

        await Task.Delay(delay);

        return ServiceActionResult<UserProfileItem>.OK(item);
    }

    public async Task<ServiceActionResult<UserProfileItem>> CreateUserAsync(UserProfileItem newUser, string role)
    {
        newUser.Id = IdGenerator.New();
        newUser.Roles = role;
        newUser.Status = "Initiated";
        newUser.CreateDate = DateTime.UtcNow;
        newUser.UpdateDate = DateTime.UtcNow;
        modelList.Add(newUser);

        await Task.Delay(delay);

        return ServiceActionResult<UserProfileItem>.OK(newUser);
    }

    public async Task<ServiceActionResult<UserProfileItem>> UpdateAsync(string id, UserProfileItem item)
    {
        var index = modelList.FindIndex(i => i.Id == id);
        if (index < 0) throw new InvalidOperationException("User not found.");

        item.Id = id;
        item.UpdateDate = DateTime.UtcNow;
        modelList[index] = item;

        await Task.Delay(delay);

        return ServiceActionResult<UserProfileItem>.OK(item);
    }

    public async Task<ServiceActionResult<UserProfileItem>> UpdateUserAsync(string id, UserProfileItem updatedUser, string role)
    {
        var index = modelList.FindIndex(i => i.Id == id);
        if (index < 0) throw new InvalidOperationException("User not found.");

        updatedUser.Id = id;
        updatedUser.Roles = role;
        updatedUser.UpdateDate = DateTime.UtcNow;
        modelList[index] = updatedUser;

        await Task.Delay(delay);

        return ServiceActionResult<UserProfileItem>.OK(updatedUser);
    }

    public async Task<ServiceActionResult<UserProfileItem>> DeleteAsync(string id)
    {
        var item = modelList.Find(i => i.Id == id);
        if (item == null) throw new InvalidOperationException("User not found.");

        modelList.Remove(item);

        await Task.Delay(delay);

        return ServiceActionResult<UserProfileItem>.OK(item);
    }

    public async Task<ServiceActionResult<bool>> DeleteUserAsync(string id)
    {
        var item = modelList.Find(i => i.Id == id);
        if (item == null) throw new InvalidOperationException("User not found.");

        modelList.Remove(item);

        await Task.Delay(delay);

        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<List<UserOrganizationItem>>> GetUserOrganizationsAsync(string userId)
    {
        await Task.Delay(delay);

        var items = userOrganizations.GetValueOrDefault(userId, new List<UserOrganizationItem>());
        return ServiceActionResult<List<UserOrganizationItem>>.OK(items);
    }

    public async Task<ServiceActionResult<bool>> SetUserOrganizationsAsync(string userId, List<UserOrganizationItem> organizations)
    {
        await Task.Delay(delay);

        // Enrich with org names from mock data
        foreach (var org in organizations)
        {
            var mockOrg = mockOrgs.FirstOrDefault(o => o.Id == org.OrganizationId);
            if (mockOrg != default)
            {
                org.OrganizationName = mockOrg.Name;
                org.OrganizationPath = mockOrg.Path;
            }
        }

        userOrganizations[userId] = organizations;
        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<List<PermissionOverrideItem>>> GetUserPermissionOverridesAsync(string userId)
    {
        await Task.Delay(delay);

        var items = userOverrides.GetValueOrDefault(userId, new List<PermissionOverrideItem>());
        return ServiceActionResult<List<PermissionOverrideItem>>.OK(items);
    }

    public async Task<ServiceActionResult<bool>> SetUserPermissionOverridesAsync(string userId, List<PermissionOverrideItem> overrides)
    {
        await Task.Delay(delay);

        userOverrides[userId] = overrides;
        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<UserProfileItem>> UploadProfilePictureAsync(string userId, Stream imageStream, string contentType)
    {
        await Task.Delay(delay);

        var user = modelList.Find(i => i.Id == userId);
        if (user == null) throw new InvalidOperationException("User not found.");

        user.HasProfilePicture = true;
        user.ProfilePictureUrl = $"api/users/{userId}/profile-picture";
        user.ProfilePictureThumbnailUrl = $"api/users/{userId}/profile-picture/thumbnail";

        return ServiceActionResult<UserProfileItem>.OK(user);
    }

    public async Task<ServiceActionResult<bool>> DeleteProfilePictureAsync(string userId)
    {
        await Task.Delay(delay);

        var user = modelList.Find(i => i.Id == userId);
        if (user == null) throw new InvalidOperationException("User not found.");

        user.HasProfilePicture = false;
        user.ProfilePictureUrl = null;
        user.ProfilePictureThumbnailUrl = null;

        return ServiceActionResult<bool>.OK(true);
    }

    public async Task<ServiceActionResult<(byte[] Data, string ContentType)>> GetProfilePictureAsync(string userId)
    {
        await Task.Delay(delay);
        throw new InvalidOperationException("Profile picture not available in mock mode.");
    }

    public async Task<ServiceActionResult<(byte[] Data, string ContentType)>> GetProfilePictureThumbnailAsync(string userId)
    {
        await Task.Delay(delay);
        throw new InvalidOperationException("Profile picture thumbnail not available in mock mode.");
    }

    public async Task<ServiceActionResult<List<EffectivePermissionItem>>> GetEffectivePermissionsAsync(string userId)
    {
        await Task.Delay(delay);

        var user = modelList.Find(i => i.Id == userId);
        var userRole = user?.Roles?.Split(',').FirstOrDefault()?.Trim() ?? "";
        var overrides = userOverrides.GetValueOrDefault(userId, new List<PermissionOverrideItem>());
        var overrideLookup = overrides.ToDictionary(o => o.PermissionKey, o => o);

        var allKeys = PermissionDefinitions.GetAll();
        var isAdminOrOwner = userRole is "Owner" or "Admin";

        var items = allKeys.Select(key =>
        {
            var parts = key.Split('.');
            var module = parts.Length > 0 ? parts[0] : "";
            var entity = parts.Length > 1 ? parts[1] : "";
            var action = parts.Length > 2 ? parts[2] : "";

            var hasOverride = overrideLookup.TryGetValue(key, out var ov);

            bool isGranted;
            string source;

            if (hasOverride && ov!.IsGranted)
            {
                isGranted = true;
                source = isAdminOrOwner
                    ? $"Override: Granted (also via Role: {userRole})"
                    : "Override: Granted";
            }
            else if (hasOverride && !ov!.IsGranted)
            {
                isGranted = false;
                source = isAdminOrOwner
                    ? $"Override: Denied (overrides Role: {userRole})"
                    : "Override: Denied";
            }
            else if (isAdminOrOwner)
            {
                isGranted = true;
                source = $"Role: {userRole}";
            }
            else if (action == "View")
            {
                isGranted = true;
                source = $"Role: {userRole}";
            }
            else
            {
                isGranted = false;
                source = "Not granted";
            }

            return new EffectivePermissionItem
            {
                Key = key,
                Module = module,
                Entity = entity,
                Action = action,
                IsGranted = isGranted,
                Source = source
            };
        }).ToList();

        return ServiceActionResult<List<EffectivePermissionItem>>.OK(items);
    }
}
