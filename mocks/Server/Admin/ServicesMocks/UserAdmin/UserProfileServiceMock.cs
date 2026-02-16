using Bogus;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Admin.Services.UserAdmin;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.WebServiceToolkit.Common.Model;
using DevInstance.WebServiceToolkit.Common.Tools;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Mocks.UserAdmin;

[BlazorServiceMock]
public class UserProfileServiceMock : IUserProfileService
{
    const int TotalCount = 100;
    List<UserProfileItem> modelList = new List<UserProfileItem>();

    private int delay = 500;

    public UserProfileServiceMock()
    {
        var faker = CreateFaker();
        modelList = faker.Generate(TotalCount);
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

        if (!string.IsNullOrEmpty(search))
        {
            var searchResult = modelList
                .Where(u => u.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase)
                         || u.LastName.Contains(search, StringComparison.OrdinalIgnoreCase)
                         || u.Email.Contains(search, StringComparison.OrdinalIgnoreCase)
                         || (u.PhoneNumber != null && u.PhoneNumber.Contains(search, StringComparison.OrdinalIgnoreCase)))
                .Take(topVal)
                .Select(u => new UserProfileItem
                {
                    Id = u.Id,
                    Email = u.Email.Replace(search, $"<mark>{search}</mark>", StringComparison.OrdinalIgnoreCase),
                    FirstName = u.FirstName.Replace(search, $"<mark>{search}</mark>", StringComparison.OrdinalIgnoreCase),
                    MiddleName = u.MiddleName,
                    LastName = u.LastName.Replace(search, $"<mark>{search}</mark>", StringComparison.OrdinalIgnoreCase),
                    PhoneNumber = u.PhoneNumber?.Replace(search, $"<mark>{search}</mark>", StringComparison.OrdinalIgnoreCase) ?? "",
                    Roles = u.Roles,
                    Status = u.Status,
                    CreateDate = u.CreateDate,
                    UpdateDate = u.UpdateDate
                })
                .ToList();

            return ServiceActionResult<ModelList<UserProfileItem>>.OK(
                ModelListResult.CreateList(searchResult.ToArray(), searchResult.Count, topVal, pageVal, sortBy, search, true));
        }

        var items = modelList
            .Skip(pageVal * topVal)
            .Take(topVal)
            .ToArray();

        await Task.Delay(delay);

        return ServiceActionResult<ModelList<UserProfileItem>>.OK(
            ModelListResult.CreateList(items, modelList.Count, topVal, pageVal, sortBy, search));
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
}
