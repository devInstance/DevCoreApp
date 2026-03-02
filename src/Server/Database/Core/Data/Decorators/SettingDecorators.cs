using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Model.Settings;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;

public static class SettingDecorators
{
    public static SettingItem ToView(this Setting setting)
    {
        string scope;
        if (setting.UserId != null)
            scope = "User";
        else if (setting.OrganizationId != null)
            scope = "Organization";
        else if (setting.TenantId != null)
            scope = "Tenant";
        else
            scope = "System";

        return new SettingItem
        {
            Id = setting.Id.ToString(),
            Category = setting.Category,
            Key = setting.Key,
            Value = setting.IsSensitive ? "" : setting.Value,
            ValueType = setting.ValueType,
            Description = setting.Description,
            IsSensitive = setting.IsSensitive,
            Scope = scope,
            OrganizationId = setting.OrganizationId?.ToString(),
        };
    }

    public static Setting ToRecord(this Setting setting, SettingItem item)
    {
        setting.Category = item.Category;
        setting.Key = item.Key;
        setting.Value = item.Value;
        setting.ValueType = item.ValueType;
        setting.Description = item.Description;
        setting.IsSensitive = item.IsSensitive;
        return setting;
    }
}
