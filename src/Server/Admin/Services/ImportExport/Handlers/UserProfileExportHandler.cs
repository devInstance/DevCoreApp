using DevInstance.DevCoreApp.Server.Admin.Services.UserAdmin;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.DevCoreApp.Shared.Model.ImportExport;
using Microsoft.Extensions.DependencyInjection;

namespace DevInstance.DevCoreApp.Server.Admin.Services.ImportExport.Handlers;

public class UserProfileExportHandler : IExportHandler<UserProfileItem>
{
    public string EntityType => "UserProfile";

    public List<ExportFieldDescriptor> GetFieldDescriptors()
    {
        return new List<ExportFieldDescriptor>
        {
            new() { Field = "Email", Label = "Email", IsDefault = true },
            new() { Field = "FirstName", Label = "First Name", IsDefault = true },
            new() { Field = "MiddleName", Label = "Middle Name", IsDefault = false },
            new() { Field = "LastName", Label = "Last Name", IsDefault = true },
            new() { Field = "PhoneNumber", Label = "Phone Number", IsDefault = true },
            new() { Field = "Roles", Label = "Roles", IsDefault = true },
            new() { Field = "Status", Label = "Status", IsDefault = true }
        };
    }

    public async Task<List<Dictionary<string, string?>>> GetExportDataAsync(
        List<string> selectedFields, string? search, string[]? sortBy, IServiceProvider scopedProvider)
    {
        var userService = scopedProvider.GetRequiredService<IUserProfileService>();
        var allRows = new List<Dictionary<string, string?>>();

        int page = 0;
        const int pageSize = 100;
        bool hasMore = true;

        while (hasMore)
        {
            var result = await userService.GetListAsync(pageSize, page, sortBy, search);
            var list = result.Result;

            if (list?.Items == null || list.Items.Length == 0)
                break;

            foreach (var user in list.Items)
            {
                var row = new Dictionary<string, string?>();

                foreach (var field in selectedFields)
                {
                    row[field] = field switch
                    {
                        "Email" => user.Email,
                        "FirstName" => user.FirstName,
                        "MiddleName" => user.MiddleName,
                        "LastName" => user.LastName,
                        "PhoneNumber" => user.PhoneNumber,
                        "Roles" => user.Roles,
                        "Status" => user.Status,
                        _ => null
                    };
                }

                allRows.Add(row);
            }

            hasMore = list.Items.Length == pageSize;
            page++;
        }

        return allRows;
    }
}
