using System.Text.RegularExpressions;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Admin.Services.UserAdmin;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.DevCoreApp.Shared.Model.ImportExport;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevInstance.DevCoreApp.Server.Admin.Services.ImportExport.Handlers;

public class UserProfileImportHandler : IImportHandler<UserProfileItem>
{
    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        ApplicationRoles.Admin,
        ApplicationRoles.Manager,
        ApplicationRoles.Employee,
        ApplicationRoles.Client
    };

    public string EntityType => "UserProfile";
    public string? UniqueKeyField => "Email";

    public List<ImportFieldDescriptor> GetFieldDescriptors()
    {
        return new List<ImportFieldDescriptor>
        {
            new() { Field = "Email", Label = "Email", IsRequired = true, DataType = "email", Description = "User email address" },
            new() { Field = "FirstName", Label = "First Name", IsRequired = true, DataType = "string" },
            new() { Field = "MiddleName", Label = "Middle Name", IsRequired = false, DataType = "string" },
            new() { Field = "LastName", Label = "Last Name", IsRequired = true, DataType = "string" },
            new() { Field = "PhoneNumber", Label = "Phone Number", IsRequired = false, DataType = "phone" },
            new() { Field = "Role", Label = "Role", IsRequired = true, DataType = "string", Description = "Admin, Manager, Employee, or Client" }
        };
    }

    public async Task<ImportRowValidation> ValidateRowAsync(Dictionary<string, string?> mappedValues, IServiceProvider scopedProvider)
    {
        var validation = new ImportRowValidation();

        mappedValues.TryGetValue("Email", out var email);
        mappedValues.TryGetValue("FirstName", out var firstName);
        mappedValues.TryGetValue("LastName", out var lastName);
        mappedValues.TryGetValue("PhoneNumber", out var phone);
        mappedValues.TryGetValue("Role", out var role);

        if (string.IsNullOrWhiteSpace(email))
            validation.Errors.Add("Email is required.");
        else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            validation.Errors.Add("Email format is invalid.");

        if (string.IsNullOrWhiteSpace(firstName))
            validation.Errors.Add("First Name is required.");

        if (string.IsNullOrWhiteSpace(lastName))
            validation.Errors.Add("Last Name is required.");

        if (!string.IsNullOrWhiteSpace(phone) && !Regex.IsMatch(phone, @"^[\d\s\+\-\(\)]+$"))
            validation.Errors.Add("Phone number format is invalid.");

        if (string.IsNullOrWhiteSpace(role))
            validation.Errors.Add("Role is required.");
        else if (!ValidRoles.Contains(role))
            validation.Errors.Add($"Role must be one of: {string.Join(", ", ValidRoles)}.");

        // Check if user already exists (for Create vs Update detection)
        if (!string.IsNullOrWhiteSpace(email) && validation.Errors.Count == 0)
        {
            var userManager = scopedProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                validation.Action = ImportRowAction.Update;
                validation.Warnings.Add($"User with email '{email}' already exists and will be updated.");
            }
        }

        return validation;
    }

    public async Task<ImportCommitResult> CommitAsync(List<Dictionary<string, string?>> validRows, IServiceProvider scopedProvider)
    {
        var userService = scopedProvider.GetRequiredService<IUserProfileService>();
        var userManager = scopedProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var repository = scopedProvider.GetRequiredService<IQueryRepository>();
        var authContext = scopedProvider.GetRequiredService<IAuthorizationContext>();
        var result = new ImportCommitResult();

        foreach (var row in validRows)
        {
            try
            {
                row.TryGetValue("Email", out var email);
                row.TryGetValue("FirstName", out var firstName);
                row.TryGetValue("MiddleName", out var middleName);
                row.TryGetValue("LastName", out var lastName);
                row.TryGetValue("PhoneNumber", out var phone);
                row.TryGetValue("Role", out var role);

                var existingAppUser = !string.IsNullOrWhiteSpace(email)
                    ? await userManager.FindByEmailAsync(email)
                    : null;

                if (existingAppUser != null)
                {
                    // Look up UserProfile by ApplicationUserId to get PublicId
                    var profileQuery = repository.GetUserProfilesQuery(authContext.CurrentProfile);
                    var profile = await profileQuery.ByApplicationUserId(existingAppUser.Id).Select().FirstOrDefaultAsync();

                    if (profile == null)
                    {
                        result.ErrorRows++;
                        result.Errors.Add($"Row with email '{email}': User profile not found.");
                        continue;
                    }

                    var userItem = new UserProfileItem
                    {
                        Email = email ?? string.Empty,
                        FirstName = firstName ?? string.Empty,
                        MiddleName = middleName,
                        LastName = lastName ?? string.Empty,
                        PhoneNumber = phone
                    };

                    var updateResult = await userService.UpdateUserAsync(profile.PublicId, userItem, role ?? "Employee");
                    if (updateResult.Result != null)
                    {
                        result.UpdatedRows++;
                        result.ImportedRecordIds.Add(updateResult.Result.Id);
                    }
                    else
                    {
                        result.ErrorRows++;
                        result.Errors.Add($"Row with email '{email}': Failed to update user.");
                    }
                }
                else
                {
                    var userItem = new UserProfileItem
                    {
                        Email = email ?? string.Empty,
                        FirstName = firstName ?? string.Empty,
                        MiddleName = middleName,
                        LastName = lastName ?? string.Empty,
                        PhoneNumber = phone
                    };

                    var createResult = await userService.CreateUserAsync(userItem, role ?? "Employee");
                    if (createResult.Result != null)
                    {
                        result.ImportedRows++;
                        result.ImportedRecordIds.Add(createResult.Result.Id);
                    }
                    else
                    {
                        result.ErrorRows++;
                        result.Errors.Add($"Row with email '{email}': Failed to create user.");
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorRows++;
                row.TryGetValue("Email", out var email);
                result.Errors.Add($"Row with email '{email}': {ex.Message}");
            }
        }

        return result;
    }

    public async Task RollbackAsync(List<string> recordIds, IServiceProvider scopedProvider)
    {
        var userService = scopedProvider.GetRequiredService<IUserProfileService>();

        foreach (var id in recordIds)
        {
            await userService.DeleteUserAsync(id);
        }
    }
}
