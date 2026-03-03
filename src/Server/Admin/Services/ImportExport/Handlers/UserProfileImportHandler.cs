using System.Text.RegularExpressions;
using DevInstance.DevCoreApp.Server.Admin.Services.Authentication;
using DevInstance.DevCoreApp.Server.Admin.Services.UserAdmin;
using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.DevCoreApp.Shared.Model.ImportExport;
using Microsoft.AspNetCore.Identity;
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

    public async Task<List<string>> ValidateRowAsync(Dictionary<string, string?> mappedValues, IServiceProvider scopedProvider)
    {
        var errors = new List<string>();

        mappedValues.TryGetValue("Email", out var email);
        mappedValues.TryGetValue("FirstName", out var firstName);
        mappedValues.TryGetValue("LastName", out var lastName);
        mappedValues.TryGetValue("PhoneNumber", out var phone);
        mappedValues.TryGetValue("Role", out var role);

        if (string.IsNullOrWhiteSpace(email))
            errors.Add("Email is required.");
        else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            errors.Add("Email format is invalid.");

        if (string.IsNullOrWhiteSpace(firstName))
            errors.Add("First Name is required.");

        if (string.IsNullOrWhiteSpace(lastName))
            errors.Add("Last Name is required.");

        if (!string.IsNullOrWhiteSpace(phone) && !Regex.IsMatch(phone, @"^[\d\s\+\-\(\)]+$"))
            errors.Add("Phone number format is invalid.");

        if (string.IsNullOrWhiteSpace(role))
            errors.Add("Role is required.");
        else if (!ValidRoles.Contains(role))
            errors.Add($"Role must be one of: {string.Join(", ", ValidRoles)}.");

        if (!string.IsNullOrWhiteSpace(email) && errors.Count == 0)
        {
            var userManager = scopedProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                errors.Add($"A user with email '{email}' already exists.");
            }
        }

        return errors;
    }

    public async Task<ImportCommitResult> CommitAsync(List<Dictionary<string, string?>> validRows, IServiceProvider scopedProvider)
    {
        var userService = scopedProvider.GetRequiredService<IUserProfileService>();
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
                }
                else
                {
                    result.ErrorRows++;
                    result.Errors.Add($"Row with email '{email}': Failed to create user.");
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
}
